"""
Hydrothermal vent that spawns C tokens at the bottom of the grid.
"""

import random
from code_token import Token
from typing import Optional

class HydrothermalVent:
    """Spawns C tokens at a specific location at the bottom of the grid."""

    # Common C language tokens to spawn
    C_TOKENS = [
        # Keywords
        'if', 'else', 'for', 'while', 'return', 'int', 'float', 'char', 'void',
        'struct', 'typedef', 'sizeof', 'const', 'static', 'break', 'continue',
        # Operators
        '+', '-', '*', '/', '=', '==', '!=', '<', '>', '<=', '>=', '&&', '||',
        '++', '--', '+=', '-=', '*=', '/=', '&', '|', '^', '~', '<<', '>>',
        # Punctuation
        '(', ')', '{', '}', '[', ']', ';', ',', '.', '->', '%',
        # Common identifiers
        'main', 'printf', 'scanf', 'malloc', 'free', 'NULL', 'argc', 'argv',
        'i', 'j', 'k', 'x', 'y', 'z', 'n', 'count', 'sum', 'temp', 'result',
        # Literals
        '0', '1', '2', '10', '100', '0x00', '0xFF',
    ]

    def __init__(self, x: int, y: int, z: int = 0, spawn_rate: int = 10):
        """
        Initialize the vent at position (x, y, z).
        spawn_rate: spawns 1 token every this many ticks
        """
        self.x = x
        self.y = y
        self.z = z  # Bottom layer
        self.spawn_rate = spawn_rate
        self.tick_counter = 0
        self.tokens_spawned = 0

    def update(self, tick: int) -> Optional[Token]:
        """
        Update the vent. Returns a new token if it's time to spawn one.
        """
        self.tick_counter += 1

        if self.tick_counter >= self.spawn_rate:
            self.tick_counter = 0
            return self.spawn_token()

        return None

    def spawn_token(self) -> Token:
        """Spawn a new C token with energy of 50."""
        # Randomly select a C token
        token_value = random.choice(self.C_TOKENS)

        # Create token at vent position with 50 energy
        token = Token(
            value=token_value,
            x=float(self.x),
            y=float(self.y),
            z=float(self.z),
            energy=50
        )

        self.tokens_spawned += 1
        return token

    def __repr__(self):
        return f"Vent(pos=({self.x},{self.y},{self.z}), spawned={self.tokens_spawned})"
