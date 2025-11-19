"""
Physics engine for the 3D code biochemistry simulation.
Handles gravity, rising/sinking, collisions, and token movement.
"""

from typing import List, Tuple, Set
from code_token import Token
from grid import Grid, Cell
import random

class PhysicsEngine:
    """Manages all physics interactions in the simulation."""

    def __init__(self, grid: Grid):
        self.grid = grid

    def update_all_tokens(self, tick: int):
        """Update physics for all tokens in the simulation."""
        # Create a copy of tokens to iterate over (in case tokens are removed)
        tokens_to_update = list(self.grid.all_tokens)

        for token in tokens_to_update:
            if token not in self.grid.all_tokens:
                continue  # Token was removed during this tick

            # Store old position
            old_x, old_y, old_z = token.x, token.y, token.z

            # Update token position based on energy
            token.update_position(tick)

            # Check collisions and handle rising/sinking interactions
            self._handle_collisions(token, old_z)

            # Apply gravity pull toward lowest nearby point
            self._apply_gravity_pull(token)

            # Try to move token to new position
            new_x, new_y, new_z = token.x, token.y, token.z

            # Ensure token stays within bounds
            new_x = max(0, min(self.grid.size_x - 1, new_x))
            new_y = max(0, min(self.grid.size_y - 1, new_y))
            new_z = max(0, min(self.grid.size_z - 1, new_z))

            # Remove tokens that sink below bottom
            if new_z < 0:
                self.grid.remove_token_from_grid(token)
                continue

            # Move token in grid
            success = self.grid.move_token(token, new_x, new_y, new_z)

            if not success:
                # Couldn't move - revert position
                token.x, token.y, token.z = old_x, old_y, old_z

    def _handle_collisions(self, rising_token: Token, old_z: float):
        """
        Handle collisions between rising and sinking tokens.
        When a rising token encounters a sinking token:
        - 1 energy is subtracted from the rising token
        - The sinking token is pushed to an adjacent cell
        """
        if rising_token.energy <= 0:
            return  # Not rising

        # Check if token is moving up
        if rising_token.z <= old_z:
            return  # Not moving up

        # Get cell at current position
        cell = self.grid.get_cell_from_float(rising_token.x, rising_token.y, rising_token.z)
        if not cell:
            return

        # Check for sinking tokens in the same cell
        for other_token in list(cell.tokens):
            if other_token == rising_token:
                continue

            # Is this token sinking?
            if other_token.energy <= 0 and other_token.z < rising_token.z:
                # Collision detected!
                # Subtract energy from rising token
                rising_token.energy -= 1

                # Push sinking token to adjacent cell
                self._push_token_aside(other_token, cell)

    def _push_token_aside(self, token: Token, current_cell: Cell):
        """Push a token to one of 8 adjacent horizontal cells."""
        # Get adjacent cells in horizontal plane
        adjacent_cells = []
        for dx in [-1, 0, 1]:
            for dy in [-1, 0, 1]:
                if dx == 0 and dy == 0:
                    continue
                cell = self.grid.get_cell(
                    current_cell.x + dx,
                    current_cell.y + dy,
                    current_cell.z
                )
                if cell and cell.can_accept(token):
                    adjacent_cells.append(cell)

        if adjacent_cells:
            # Randomly choose one
            target_cell = random.choice(adjacent_cells)
            self.grid.move_token(
                token,
                float(target_cell.x),
                float(target_cell.y),
                float(target_cell.z)
            )

    def _apply_gravity_pull(self, token: Token):
        """
        Apply gravity - pull token toward lowest nearby point.
        Gravity attempts to pull tokens to the lowest point nearby.
        """
        if token.energy > 0:
            # Rising tokens resist gravity
            return

        # Get current cell
        current_cell = self.grid.get_cell_from_float(token.x, token.y, token.z)
        if not current_cell:
            return

        # Find lowest adjacent cell
        lowest_cell = None
        lowest_z = current_cell.z

        # Check adjacent cells including diagonals
        for dx in [-1, 0, 1]:
            for dy in [-1, 0, 1]:
                for dz in [-1, 0, 1]:
                    if dx == 0 and dy == 0 and dz == 0:
                        continue

                    cell = self.grid.get_cell(
                        current_cell.x + dx,
                        current_cell.y + dy,
                        current_cell.z + dz
                    )

                    if cell and cell.z < lowest_z and cell.can_accept(token):
                        lowest_z = cell.z
                        lowest_cell = cell

        # Add velocity toward lowest point
        if lowest_cell:
            # Calculate direction toward lowest cell
            dx = lowest_cell.x - token.x
            dy = lowest_cell.y - token.y
            dz = lowest_cell.z - token.z

            # Normalize and apply
            distance = (dx**2 + dy**2 + dz**2) ** 0.5
            if distance > 0:
                token.vx += 0.1 * dx / distance
                token.vy += 0.1 * dy / distance
                token.vz += 0.1 * dz / distance

    def handle_spillover(self):
        """Handle spillover when cells exceed capacity."""
        for x in range(self.grid.size_x):
            for y in range(self.grid.size_y):
                for z in range(self.grid.size_z):
                    cell = self.grid.cells[x, y, z]

                    if cell.current_mass > Cell.MAX_CAPACITY:
                        # Need to spill over
                        self._redistribute_tokens(cell)

    def _redistribute_tokens(self, cell: Cell):
        """Redistribute tokens from an overfull cell to adjacent cells."""
        while cell.current_mass > Cell.MAX_CAPACITY and cell.tokens:
            # Remove lightest token
            lightest_token = min(cell.tokens, key=lambda t: t.mass)

            # Find adjacent cell with capacity
            adjacent = self.grid._find_adjacent_cell(
                float(cell.x),
                float(cell.y),
                float(cell.z),
                cell
            )

            if adjacent:
                self.grid.move_token(
                    lightest_token,
                    float(adjacent.x),
                    float(adjacent.y),
                    float(adjacent.z)
                )
            else:
                # No room - remove token from simulation
                self.grid.remove_token_from_grid(lightest_token)
                break

    def get_statistics(self) -> dict:
        """Get physics statistics for monitoring."""
        rising_tokens = sum(1 for t in self.grid.all_tokens if t.energy > 0)
        sinking_tokens = sum(1 for t in self.grid.all_tokens if t.energy <= 0)

        total_energy = sum(t.energy for t in self.grid.all_tokens)

        # Average altitude
        avg_z = sum(t.z for t in self.grid.all_tokens) / len(self.grid.all_tokens) if self.grid.all_tokens else 0

        return {
            'total_tokens': len(self.grid.all_tokens),
            'rising_tokens': rising_tokens,
            'sinking_tokens': sinking_tokens,
            'total_energy': total_energy,
            'average_altitude': avg_z
        }
