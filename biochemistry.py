"""
Token biochemistry engine - handles token combination, validation, and energy generation.
Acts as a simplified AST validator for determining if tokens can combine.
"""

from typing import List, Tuple, Optional, Set
from code_token import Token
from grid import Grid, Cell
import random

class TokenChain:
    """Represents a chain of bonded tokens forming code."""

    _next_chain_id = 0

    def __init__(self, head_token: Token):
        self.chain_id = TokenChain._next_chain_id
        TokenChain._next_chain_id += 1

        self.head = head_token
        self.tokens: List[Token] = [head_token]
        head_token.chain_id = self.chain_id

        self.is_valid = True  # Grammar validity
        self.last_validated_tick = 0

    def add_token(self, token: Token) -> int:
        """Add a token to the chain. Returns energy generated."""
        # Find the tail of the chain
        tail = self.head
        while tail.next_token:
            tail = tail.next_token

        # Bond the new token
        energy = tail.bond_to(token, self.chain_id)
        self.tokens.append(token)

        return energy

    def remove_token(self, token: Token):
        """Remove a token from the chain (due to grammar error)."""
        if token in self.tokens:
            self.tokens.remove(token)

            # Break bonds
            if token.prev_token:
                token.prev_token.break_bond()
            if token.next_token:
                token.next_token.prev_token = None

            token.chain_id = None

    def get_code_string(self) -> str:
        """Get the code represented by this chain."""
        result = []
        current = self.head
        while current:
            result.append(current.value)
            current = current.next_token
        return ' '.join(result)

    def get_total_mass(self) -> int:
        """Get total mass of the chain."""
        return sum(t.mass for t in self.tokens)

    def get_total_energy(self) -> int:
        """Get total energy of all tokens in chain."""
        return sum(t.energy for t in self.tokens)

    def __repr__(self):
        return f"Chain({self.chain_id}): '{self.get_code_string()}' ({len(self.tokens)} tokens)"


class BiochemistryEngine:
    """Manages token combination, validation, and energy generation."""

    def __init__(self, grid: Grid):
        self.grid = grid
        self.chains: List[TokenChain] = []
        self.next_chain_id = 0

    def update_all_chains(self, tick: int):
        """Update all token chains - attempt combinations and validate."""
        # Try to form new bonds
        self._attempt_token_combinations()

        # Validate existing chains
        self._validate_chains(tick)

        # Clean up empty chains
        self.chains = [c for c in self.chains if c.tokens]

    def _attempt_token_combinations(self):
        """Attempt to combine tokens in the same cell."""
        for x in range(self.grid.size_x):
            for y in range(self.grid.size_y):
                for z in range(self.grid.size_z):
                    cell = self.grid.cells[x, y, z]

                    if len(cell.tokens) < 2:
                        continue

                    # Try to combine tokens in this cell
                    self._combine_tokens_in_cell(cell)

    def _combine_tokens_in_cell(self, cell: Cell):
        """Attempt to combine tokens within a single cell."""
        # Get free tokens (not in a chain)
        free_tokens = [t for t in cell.tokens if t.chain_id is None and not t.metadata['damaged']]

        if not free_tokens:
            return

        # Try to find bonding pairs
        for i, token1 in enumerate(free_tokens):
            for token2 in free_tokens[i+1:]:
                can_bond, strength = token1.can_bond_with(token2)

                if can_bond and strength > 50:  # Only bond if strong enough
                    # Create a new chain or add to existing chain
                    self._create_or_extend_chain(token1, token2)
                    break

        # Try to extend existing chains
        chains_in_cell = self._get_chains_in_cell(cell)
        for chain in chains_in_cell:
            for token in free_tokens:
                if token.chain_id is not None:
                    continue

                # Try to bond to the tail of the chain
                tail = chain.head
                while tail.next_token:
                    tail = tail.next_token

                can_bond, strength = tail.can_bond_with(token)
                if can_bond and strength > 50:
                    energy = chain.add_token(token)
                    # Add energy to the chain's tokens
                    self._distribute_energy(chain, energy)
                    break

    def _create_or_extend_chain(self, token1: Token, token2: Token):
        """Create a new chain from two tokens."""
        chain = TokenChain(token1)
        energy = chain.add_token(token2)
        self.chains.append(chain)

        # Distribute energy
        self._distribute_energy(chain, energy)

    def _distribute_energy(self, chain: TokenChain, energy: int):
        """Distribute generated energy to tokens in the chain."""
        # Distribute energy evenly (or to lowest-energy tokens)
        tokens_sorted = sorted(chain.tokens, key=lambda t: t.energy)
        for token in tokens_sorted[:energy]:
            token.energy += 1

    def _get_chains_in_cell(self, cell: Cell) -> List[TokenChain]:
        """Get all chains that have tokens in this cell."""
        chain_ids = set()
        for token in cell.tokens:
            if token.chain_id is not None:
                chain_ids.add(token.chain_id)

        return [c for c in self.chains if c.chain_id in chain_ids]

    def _validate_chains(self, tick: int):
        """Validate token chains using grammar rules."""
        for chain in self.chains:
            # Validate every 10 ticks
            if tick - chain.last_validated_tick < 10:
                continue

            chain.last_validated_tick = tick

            # Simple grammar validation
            is_valid, errors = self._validate_grammar(chain)

            if not is_valid:
                # Break invalid connections
                self._fix_grammar_errors(chain, errors)

    def _validate_grammar(self, chain: TokenChain) -> Tuple[bool, List[Tuple[Token, Token]]]:
        """
        Validate the grammar of a token chain.
        Returns (is_valid, list_of_invalid_pairs)
        """
        errors = []
        current = chain.head

        while current and current.next_token:
            next_token = current.next_token

            # Check if these two tokens can validly bond
            can_bond, strength = current.can_bond_with(next_token)

            if not can_bond or strength < 30:  # Weak bonds break under validation
                errors.append((current, next_token))

            # Check for specific grammar rules
            if not self._check_grammar_rule(current, next_token):
                errors.append((current, next_token))

            current = next_token

        return (len(errors) == 0, errors)

    def _check_grammar_rule(self, token1: Token, token2: Token) -> bool:
        """Check specific grammar rules between two tokens."""
        # Matching pairs must be properly matched
        if token1.value in Token.MATCHING_PAIRS:
            # ( must eventually be followed by )
            # For simplicity, we'll allow any tokens between them
            pass

        # Type declarations must be followed by identifiers
        if token1.value in {'int', 'float', 'char', 'void'}:
            if token2.metadata['type'] not in {'identifier', 'punctuation'}:
                return False

        # Operators must be surrounded by operands
        if token1.metadata['type'] == 'operator' and token1.value not in {'++', '--'}:
            if token2.metadata['type'] not in {'identifier', 'literal', 'punctuation'}:
                return False

        return True

    def _fix_grammar_errors(self, chain: TokenChain, errors: List[Tuple[Token, Token]]):
        """
        Fix grammar errors by breaking invalid bonds.
        The system tries to reconnect or removes tokens.
        """
        for token1, token2 in errors:
            # Break the bond
            token1.break_bond()

            # Try to find a new token to connect to
            cell = self.grid.get_cell_from_float(token1.x, token1.y, token1.z)
            if cell:
                reconnected = False
                for other_token in cell.tokens:
                    if other_token == token1 or other_token == token2:
                        continue

                    can_bond, strength = token1.can_bond_with(other_token)
                    if can_bond and strength > 50:
                        # Reconnect to this token
                        token1.bond_to(other_token, chain.chain_id)
                        reconnected = True
                        break

                if not reconnected:
                    # Remove token from chain
                    chain.remove_token(token2)

    def insert_default_values(self):
        """Insert small default values into chains that need them."""
        for chain in self.chains:
            # Look for patterns that need values
            current = chain.head
            while current and current.next_token:
                next_token = current.next_token

                # Pattern: type identifier = ?
                if (current.metadata['type'] == 'operator' and
                    current.value == '=' and
                    next_token.metadata['type'] == 'punctuation'):

                    # Need a value here
                    default_value = self._generate_default_value(chain)
                    if default_value:
                        # Insert the value token
                        value_token = Token(
                            value=str(default_value),
                            x=current.x,
                            y=current.y,
                            z=current.z,
                            energy=10
                        )
                        self.grid.add_token_to_grid(value_token)

                current = next_token

    def _generate_default_value(self, chain: TokenChain) -> Optional[int]:
        """Generate a small default value for a chain."""
        # Simple default values
        return random.choice([0, 1, 2, 5, 10])

    def get_statistics(self) -> dict:
        """Get biochemistry statistics."""
        total_chains = len(self.chains)
        valid_chains = sum(1 for c in self.chains if c.is_valid)
        total_chain_length = sum(len(c.tokens) for c in self.chains)

        avg_chain_length = total_chain_length / total_chains if total_chains > 0 else 0

        return {
            'total_chains': total_chains,
            'valid_chains': valid_chains,
            'average_chain_length': avg_chain_length,
            'longest_chain': max((len(c.tokens) for c in self.chains), default=0)
        }
