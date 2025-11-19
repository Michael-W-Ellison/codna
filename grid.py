"""
3D Grid system for the code biochemistry simulation.
Each cell can contain tokens up to a maximum mass capacity.
"""

from typing import List, Tuple, Optional, Set
import numpy as np
from code_token import Token

class Cell:
    """Represents a single cell in the 3D grid."""

    MAX_CAPACITY = 1000  # Maximum mass a cell can hold

    def __init__(self, x: int, y: int, z: int):
        self.x = x
        self.y = y
        self.z = z
        self.tokens: List[Token] = []
        self.current_mass = 0

    def can_accept(self, token: Token) -> bool:
        """Check if cell can accept a token based on capacity."""
        return self.current_mass + token.mass <= self.MAX_CAPACITY

    def add_token(self, token: Token) -> bool:
        """Add a token to the cell if there's capacity."""
        if self.can_accept(token):
            self.tokens.append(token)
            self.current_mass += token.mass
            return True
        return False

    def remove_token(self, token: Token) -> bool:
        """Remove a token from the cell."""
        if token in self.tokens:
            self.tokens.remove(token)
            self.current_mass -= token.mass
            return True
        return False

    def get_tokens_by_type(self, token_type: str) -> List[Token]:
        """Get all tokens of a specific type in this cell."""
        return [t for t in self.tokens if t.metadata['type'] == token_type]

    def has_mutually_exclusive_token(self, token: Token) -> Optional[Token]:
        """Check if cell contains a token mutually exclusive with the given token."""
        for existing_token in self.tokens:
            if existing_token.is_mutually_exclusive_with(token):
                return existing_token
        return None

    def get_spillover_mass(self) -> int:
        """Calculate how much mass exceeds capacity (for spillover)."""
        return max(0, self.current_mass - self.MAX_CAPACITY)

    def __repr__(self):
        return f"Cell({self.x},{self.y},{self.z}): {len(self.tokens)} tokens, {self.current_mass}/{self.MAX_CAPACITY} mass"


class Grid:
    """3D grid representing the simulation environment."""

    def __init__(self, size_x: int = 100, size_y: int = 100, size_z: int = 100):
        self.size_x = size_x
        self.size_y = size_y
        self.size_z = size_z

        # Initialize 3D grid of cells
        self.cells = np.empty((size_x, size_y, size_z), dtype=object)
        for x in range(size_x):
            for y in range(size_y):
                for z in range(size_z):
                    self.cells[x, y, z] = Cell(x, y, z)

        # Track all tokens in the simulation
        self.all_tokens: Set[Token] = set()

    def is_valid_position(self, x: int, y: int, z: int) -> bool:
        """Check if position is within grid bounds."""
        return (0 <= x < self.size_x and
                0 <= y < self.size_y and
                0 <= z < self.size_z)

    def get_cell(self, x: int, y: int, z: int) -> Optional[Cell]:
        """Get cell at position, or None if out of bounds."""
        if self.is_valid_position(x, y, z):
            return self.cells[x, y, z]
        return None

    def get_cell_from_float(self, x: float, y: float, z: float) -> Optional[Cell]:
        """Get cell at floating point position (converts to int)."""
        return self.get_cell(int(x), int(y), int(z))

    def move_token(self, token: Token, new_x: float, new_y: float, new_z: float) -> bool:
        """
        Move a token to a new position.
        Returns True if successful, False if position invalid or cell full.
        """
        # Get old and new cells
        old_cell = self.get_cell_from_float(token.x, token.y, token.z)
        new_cell = self.get_cell_from_float(new_x, new_y, new_z)

        if not new_cell:
            # Out of bounds - remove token from simulation
            if old_cell:
                old_cell.remove_token(token)
            self.all_tokens.discard(token)
            return False

        # Check for mutual exclusion
        exclusive_token = new_cell.has_mutually_exclusive_token(token)
        if exclusive_token:
            # Repulsion: smaller token must move away
            if token.get_chain_mass() < exclusive_token.get_chain_mass():
                # Find alternative position
                alt_cell = self._find_adjacent_cell(new_x, new_y, new_z, new_cell)
                if alt_cell:
                    new_cell = alt_cell
                    new_x, new_y, new_z = float(alt_cell.x), float(alt_cell.y), float(alt_cell.z)
                else:
                    return False  # No room to move

        # Try to add to new cell
        if new_cell.can_accept(token):
            # Remove from old cell
            if old_cell and old_cell != new_cell:
                old_cell.remove_token(token)

            # Add to new cell
            new_cell.add_token(token)
            token.x = new_x
            token.y = new_y
            token.z = new_z
            return True
        else:
            # Cell is full - trigger spillover
            return self._handle_spillover(token, new_cell)

    def _find_adjacent_cell(self, x: float, y: float, z: float, exclude_cell: Cell) -> Optional[Cell]:
        """Find an adjacent cell with capacity (for repulsion/spillover)."""
        ix, iy, iz = int(x), int(y), int(z)

        # Check 8 adjacent cells in the horizontal plane
        for dx in [-1, 0, 1]:
            for dy in [-1, 0, 1]:
                if dx == 0 and dy == 0:
                    continue

                cell = self.get_cell(ix + dx, iy + dy, iz)
                if cell and cell != exclude_cell and cell.current_mass < Cell.MAX_CAPACITY:
                    return cell

        return None

    def _handle_spillover(self, token: Token, full_cell: Cell) -> bool:
        """Handle spillover when a cell is at capacity."""
        # Try to find an adjacent cell
        adjacent = self._find_adjacent_cell(
            float(full_cell.x),
            float(full_cell.y),
            float(full_cell.z),
            full_cell
        )

        if adjacent:
            return self.move_token(token, float(adjacent.x), float(adjacent.y), float(adjacent.z))

        return False

    def get_adjacent_cells(self, x: int, y: int, z: int) -> List[Cell]:
        """Get all valid adjacent cells (26 neighbors in 3D)."""
        adjacent = []
        for dx in [-1, 0, 1]:
            for dy in [-1, 0, 1]:
                for dz in [-1, 0, 1]:
                    if dx == 0 and dy == 0 and dz == 0:
                        continue
                    cell = self.get_cell(x + dx, y + dy, z + dz)
                    if cell:
                        adjacent.append(cell)
        return adjacent

    def add_token_to_grid(self, token: Token) -> bool:
        """Add a new token to the grid at its current position."""
        cell = self.get_cell_from_float(token.x, token.y, token.z)
        if cell and cell.add_token(token):
            self.all_tokens.add(token)
            return True
        return False

    def remove_token_from_grid(self, token: Token):
        """Remove a token from the grid entirely."""
        cell = self.get_cell_from_float(token.x, token.y, token.z)
        if cell:
            cell.remove_token(token)
        self.all_tokens.discard(token)

    def get_tokens_in_region(self, x: int, y: int, z: int, radius: int = 1) -> List[Token]:
        """Get all tokens within a certain radius of a position."""
        tokens = []
        for dx in range(-radius, radius + 1):
            for dy in range(-radius, radius + 1):
                for dz in range(-radius, radius + 1):
                    cell = self.get_cell(x + dx, y + dy, z + dz)
                    if cell:
                        tokens.extend(cell.tokens)
        return tokens

    def get_total_tokens(self) -> int:
        """Get total number of tokens in the simulation."""
        return len(self.all_tokens)

    def get_total_mass(self) -> int:
        """Get total mass of all tokens in the simulation."""
        return sum(token.mass for token in self.all_tokens)

    def __repr__(self):
        return f"Grid({self.size_x}x{self.size_y}x{self.size_z}): {self.get_total_tokens()} tokens"
