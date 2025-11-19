"""
Damage system for tokens at high altitudes.
The higher the altitude, the greater the probability of metadata corruption.
"""

from typing import List
from code_token import Token
from grid import Grid
import random

class DamageSystem:
    """Manages token damage at high altitudes."""

    def __init__(self, grid: Grid, max_altitude: int = 100):
        self.grid = grid
        self.max_altitude = max_altitude

        # Damage probability curve
        # At z=0: 0% damage
        # At z=50: ~5% damage per tick
        # At z=75: ~20% damage per tick
        # At z=100: ~50% damage per tick

    def calculate_damage_probability(self, z: float) -> float:
        """
        Calculate the probability of damage based on altitude.
        Uses an exponential curve.
        """
        if z <= 0:
            return 0.0

        # Exponential increase in damage probability
        # Formula: damage_prob = (z / max_altitude) ^ 3 * 0.5
        normalized_z = min(z / self.max_altitude, 1.0)
        damage_prob = (normalized_z ** 3) * 0.5

        return min(damage_prob, 0.5)  # Cap at 50%

    def apply_damage_to_all_tokens(self, tick: int):
        """Apply damage to all tokens based on their altitude."""
        for token in list(self.grid.all_tokens):
            if token not in self.grid.all_tokens:
                continue  # Token was removed

            # Calculate damage probability
            damage_prob = self.calculate_damage_probability(token.z)

            if damage_prob > 0:
                # Apply damage
                token.apply_damage(damage_prob)

                # If token is damaged, it may fail to bond or cause chain breaks
                if token.metadata['damaged']:
                    self._handle_damaged_token(token)

    def _handle_damaged_token(self, token: Token):
        """Handle a token that has become damaged."""
        # If token is in a chain, it may cause the chain to break
        if token.chain_id is not None:
            # High chance of breaking the bond
            if random.random() < 0.7:
                if token.prev_token:
                    token.prev_token.break_bond()
                if token.next_token:
                    token.break_bond()

                # Remove from chain
                token.chain_id = None

    def repair_damaged_tokens(self):
        """
        Occasionally repair damaged tokens if they sink to lower altitudes.
        Lower altitudes provide a "protective" environment.
        """
        for token in self.grid.all_tokens:
            if not token.metadata['damaged']:
                continue

            # Tokens at low altitudes can self-repair
            repair_prob = self._calculate_repair_probability(token.z)

            if random.random() < repair_prob:
                token.metadata['damaged'] = False

                # Try to restore type information
                token.metadata['type'] = token._determine_type()

    def _calculate_repair_probability(self, z: float) -> float:
        """Calculate probability of repair at given altitude."""
        if z >= self.max_altitude / 2:
            return 0.0

        # Repair probability increases as altitude decreases
        # At z=0: 10% repair per tick
        # At z=25: 5% repair per tick
        normalized_z = z / (self.max_altitude / 2)
        repair_prob = (1 - normalized_z) * 0.1

        return repair_prob

    def get_statistics(self) -> dict:
        """Get damage statistics."""
        damaged_tokens = sum(1 for t in self.grid.all_tokens if t.metadata['damaged'])

        # Calculate average damage by altitude zones
        zones = {
            'low (0-25)': 0,
            'medium (25-50)': 0,
            'high (50-75)': 0,
            'very_high (75-100)': 0
        }

        zone_counts = {
            'low (0-25)': 0,
            'medium (25-50)': 0,
            'high (50-75)': 0,
            'very_high (75-100)': 0
        }

        for token in self.grid.all_tokens:
            if token.z < 25:
                zone = 'low (0-25)'
            elif token.z < 50:
                zone = 'medium (25-50)'
            elif token.z < 75:
                zone = 'high (50-75)'
            else:
                zone = 'very_high (75-100)'

            zone_counts[zone] += 1
            if token.metadata['damaged']:
                zones[zone] += 1

        return {
            'damaged_tokens': damaged_tokens,
            'damage_by_zone': zones,
            'tokens_by_zone': zone_counts
        }
