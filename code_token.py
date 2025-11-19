"""
Token class representing a code token in the 3D biochemistry simulation.
Each token has mass, energy, position, and can combine with other tokens.
"""

from typing import Optional, List, Tuple
import random

class Token:
    """Represents a programming language token in the 3D space."""

    # Token categories and their combination rules
    KEYWORDS = {'if', 'else', 'for', 'while', 'function', 'class', 'return', 'int', 'float', 'var', 'let', 'const'}
    OPERATORS = {'+', '-', '*', '/', '=', '==', '!=', '<', '>', '<=', '>=', '&&', '||'}
    PUNCTUATION = {'(', ')', '{', '}', '[', ']', ';', ',', '.'}

    # Matching pairs (strong covalent bonds)
    MATCHING_PAIRS = {
        '(': ')',
        '{': '}',
        '[': ']'
    }

    def __init__(self, value: str, x: float, y: float, z: float, energy: int = 50):
        self.value = value
        self.mass = len(value)  # Mass equals number of characters
        self.energy = energy
        self.x = x
        self.y = y
        self.z = z
        self.vx = 0.0  # Velocity in x direction
        self.vy = 0.0  # Velocity in y direction
        self.vz = 0.0  # Velocity in z direction

        # Metadata for AST validation
        token_type = self._determine_type()
        self.metadata = {
            'type': token_type,
            'damaged': False,
            'bond_strength': 0,
            'expecting': None,  # What token types can follow
            'can_start_chain': token_type in {'keyword', 'identifier', 'punctuation'}
        }

        # Chain information
        self.chain_id: Optional[int] = None
        self.next_token: Optional['Token'] = None
        self.prev_token: Optional['Token'] = None

    def _determine_type(self) -> str:
        """Determine the token type based on its value."""
        if self.value in self.KEYWORDS:
            return 'keyword'
        elif self.value in self.OPERATORS:
            return 'operator'
        elif self.value in self.PUNCTUATION:
            return 'punctuation'
        elif self.value.isidentifier():
            return 'identifier'
        elif self.value.isdigit() or self._is_number():
            return 'literal'
        else:
            return 'unknown'

    def _is_number(self) -> bool:
        """Check if value is a number."""
        try:
            float(self.value)
            return True
        except ValueError:
            return False

    def get_position(self) -> Tuple[float, float, float]:
        """Return current position as tuple."""
        return (self.x, self.y, self.z)

    def update_position(self, tick: int):
        """Update position based on energy and gravity."""
        if self.energy > 0:
            # Rising: use energy to move up
            self.z += 1.0
            self.energy -= 1
        else:
            # Sinking: gravity pulls down
            self.z -= 1.0

        # Add velocity changes
        self.x += self.vx
        self.y += self.vy
        self.z += self.vz

        # Decay velocity (friction)
        self.vx *= 0.9
        self.vy *= 0.9
        self.vz *= 0.9

    def can_bond_with(self, other: 'Token') -> Tuple[bool, int]:
        """
        Determine if this token can bond with another token.
        Returns (can_bond, bond_strength)
        """
        if self.metadata['damaged'] or other.metadata['damaged']:
            # Damaged tokens have impaired bonding ability
            return (False, 0)

        my_type = self.metadata['type']
        other_type = other.metadata['type']

        # Strong covalent bonds: matching pairs
        if self.value in self.MATCHING_PAIRS:
            if other.value == self.MATCHING_PAIRS[self.value]:
                return (True, 100)  # Very strong bond

        # Keyword bonds
        if my_type == 'keyword':
            if self.value in {'if', 'while', 'for'} and other.value == '(':
                return (True, 90)
            elif self.value in {'int', 'float', 'var', 'let', 'const'} and other_type == 'identifier':
                return (True, 85)

        # Operator bonds
        if my_type == 'operator':
            if self.value == '=' and other_type in {'identifier', 'literal'}:
                return (True, 80)
            elif self.value in {'+', '-', '*', '/', '==', '!=', '<', '>'} and other_type in {'identifier', 'literal'}:
                return (True, 75)

        # Punctuation bonds
        if self.value == '(' and other_type in {'identifier', 'literal', 'keyword'}:
            return (True, 70)

        if self.value == ',' and other_type in {'identifier', 'literal'}:
            return (True, 60)

        # Sequential identifier or literal bonds (weaker)
        if my_type in {'identifier', 'literal'} and other.value in {'+', '-', '*', '/', '=', ';', ',', ')'}:
            return (True, 50)

        return (False, 0)

    def is_mutually_exclusive_with(self, other: 'Token') -> bool:
        """
        Determine if two tokens are mutually exclusive (repulsive).
        This happens when tokens serve similar functions but can't coexist.
        """
        # Keywords that are mutually exclusive in the same context
        mutually_exclusive_sets = [
            {'if', 'while', 'for'},  # Control flow starters
            {'int', 'float', 'var'},  # Type declarations
            {'++', '--'},  # Increment/decrement
            {'&&', '||'},  # Logical operators (in simple cases)
        ]

        for exclusive_set in mutually_exclusive_sets:
            if self.value in exclusive_set and other.value in exclusive_set:
                if self.value != other.value:
                    return True

        return False

    def apply_damage(self, damage_probability: float):
        """Apply damage to token metadata based on altitude."""
        if random.random() < damage_probability:
            self.metadata['damaged'] = True
            # Damage can also corrupt the type recognition
            if random.random() < 0.3:
                self.metadata['type'] = 'unknown'

    def bond_to(self, other: 'Token', chain_id: int) -> int:
        """
        Bond this token to another token in a chain.
        Returns energy generated from the bond.
        """
        self.next_token = other
        other.prev_token = self
        self.chain_id = chain_id
        other.chain_id = chain_id

        # Energy generation: 1 per token minus 1 total
        # For 2 tokens: 2 - 1 = 1 energy
        return 1

    def break_bond(self):
        """Break the bond with the next token."""
        if self.next_token:
            self.next_token.prev_token = None
            self.next_token = None

    def get_chain_length(self) -> int:
        """Get the length of the chain this token belongs to."""
        length = 1
        current = self.next_token
        while current:
            length += 1
            current = current.next_token
        return length

    def get_chain_mass(self) -> int:
        """Get the total mass of the chain this token belongs to."""
        mass = self.mass
        current = self.next_token
        while current:
            mass += current.mass
            current = current.next_token
        return mass

    def __repr__(self):
        return f"Token('{self.value}', pos=({self.x:.1f},{self.y:.1f},{self.z:.1f}), energy={self.energy}, mass={self.mass})"
