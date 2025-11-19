"""
Main simulation loop for the 3D code biochemistry system.
Coordinates all subsystems: physics, biochemistry, damage, and spawning.
"""

from grid import Grid
from vent import HydrothermalVent
from physics import PhysicsEngine
from biochemistry import BiochemistryEngine
from damage import DamageSystem
from typing import Dict, List
import time

class Simulation:
    """Main simulation coordinator."""

    def __init__(self, grid_size: int = 100, vent_position: tuple = None):
        # Initialize grid
        self.grid = Grid(grid_size, grid_size, grid_size)

        # Initialize vent at center of bottom layer
        if vent_position is None:
            vent_position = (grid_size // 2, grid_size // 2, 0)

        self.vent = HydrothermalVent(
            x=vent_position[0],
            y=vent_position[1],
            z=vent_position[2],
            spawn_rate=10  # 1 token every 10 ticks
        )

        # Initialize subsystems
        self.physics = PhysicsEngine(self.grid)
        self.biochemistry = BiochemistryEngine(self.grid)
        self.damage_system = DamageSystem(self.grid, max_altitude=grid_size)

        # Simulation state
        self.tick = 0
        self.running = False
        self.statistics_history: List[Dict] = []

    def step(self):
        """Execute one simulation tick."""
        self.tick += 1

        # 1. Spawn new tokens from vent
        new_token = self.vent.update(self.tick)
        if new_token:
            self.grid.add_token_to_grid(new_token)

        # 2. Update physics (movement, gravity, collisions)
        self.physics.update_all_tokens(self.tick)

        # 3. Handle spillover
        self.physics.handle_spillover()

        # 4. Update biochemistry (token combination, validation)
        self.biochemistry.update_all_chains(self.tick)

        # 5. Insert default values where needed
        if self.tick % 50 == 0:  # Every 50 ticks
            self.biochemistry.insert_default_values()

        # 6. Apply damage to high-altitude tokens
        self.damage_system.apply_damage_to_all_tokens(self.tick)

        # 7. Repair damaged tokens at low altitudes
        if self.tick % 20 == 0:  # Every 20 ticks
            self.damage_system.repair_damaged_tokens()

        # 8. Collect statistics
        if self.tick % 10 == 0:  # Every 10 ticks
            self._collect_statistics()

    def run(self, num_ticks: int, verbose: bool = True):
        """Run simulation for a specified number of ticks."""
        self.running = True
        start_time = time.time()

        for i in range(num_ticks):
            if not self.running:
                break

            self.step()

            if verbose and self.tick % 100 == 0:
                self._print_status()

        elapsed_time = time.time() - start_time

        if verbose:
            print(f"\nSimulation completed: {num_ticks} ticks in {elapsed_time:.2f} seconds")
            print(f"Average: {num_ticks/elapsed_time:.1f} ticks/second")
            self._print_final_statistics()

    def stop(self):
        """Stop the simulation."""
        self.running = False

    def _collect_statistics(self):
        """Collect statistics from all subsystems."""
        stats = {
            'tick': self.tick,
            'total_tokens': self.grid.get_total_tokens(),
            'total_mass': self.grid.get_total_mass(),
            'vent_spawned': self.vent.tokens_spawned,
        }

        # Physics stats
        stats.update(self.physics.get_statistics())

        # Biochemistry stats
        stats.update(self.biochemistry.get_statistics())

        # Damage stats
        stats.update(self.damage_system.get_statistics())

        self.statistics_history.append(stats)

    def _print_status(self):
        """Print current simulation status."""
        if not self.statistics_history:
            return

        stats = self.statistics_history[-1]

        print(f"\n=== Tick {self.tick} ===")
        print(f"Tokens: {stats['total_tokens']} (spawned: {stats['vent_spawned']})")
        print(f"  Rising: {stats['rising_tokens']}, Sinking: {stats['sinking_tokens']}")
        print(f"  Average altitude: {stats['average_altitude']:.1f}")
        print(f"  Total energy: {stats['total_energy']}")
        print(f"Chains: {stats['total_chains']} (valid: {stats['valid_chains']})")
        print(f"  Average length: {stats['average_chain_length']:.1f}")
        print(f"  Longest: {stats['longest_chain']}")
        print(f"Damage: {stats['damaged_tokens']} tokens damaged")

    def _print_final_statistics(self):
        """Print final simulation statistics."""
        print("\n" + "="*60)
        print("FINAL STATISTICS")
        print("="*60)

        if not self.statistics_history:
            print("No statistics collected.")
            return

        final_stats = self.statistics_history[-1]

        print(f"\nTotal ticks: {self.tick}")
        print(f"Tokens spawned: {self.vent.tokens_spawned}")
        print(f"Tokens remaining: {final_stats['total_tokens']}")
        print(f"Total mass: {final_stats['total_mass']}")

        print(f"\nPhysics:")
        print(f"  Rising tokens: {final_stats['rising_tokens']}")
        print(f"  Sinking tokens: {final_stats['sinking_tokens']}")
        print(f"  Average altitude: {final_stats['average_altitude']:.1f}")
        print(f"  Total energy: {final_stats['total_energy']}")

        print(f"\nBiochemistry:")
        print(f"  Total chains: {final_stats['total_chains']}")
        print(f"  Valid chains: {final_stats['valid_chains']}")
        print(f"  Average chain length: {final_stats['average_chain_length']:.1f}")
        print(f"  Longest chain: {final_stats['longest_chain']}")

        print(f"\nDamage:")
        print(f"  Damaged tokens: {final_stats['damaged_tokens']}")
        print(f"  By zone:")
        for zone, count in final_stats['damage_by_zone'].items():
            total = final_stats['tokens_by_zone'][zone]
            pct = (count / total * 100) if total > 0 else 0
            print(f"    {zone}: {count}/{total} ({pct:.1f}%)")

        # Print some example chains
        print(f"\nExample token chains:")
        for i, chain in enumerate(self.biochemistry.chains[:5]):
            print(f"  {i+1}. {chain}")

    def get_statistics(self) -> List[Dict]:
        """Get all collected statistics."""
        return self.statistics_history

    def get_current_state(self) -> Dict:
        """Get current state for visualization."""
        tokens_by_altitude = {}
        for token in self.grid.all_tokens:
            z = int(token.z)
            if z not in tokens_by_altitude:
                tokens_by_altitude[z] = []
            tokens_by_altitude[z].append({
                'value': token.value,
                'x': token.x,
                'y': token.y,
                'z': token.z,
                'energy': token.energy,
                'damaged': token.metadata['damaged'],
                'in_chain': token.chain_id is not None
            })

        return {
            'tick': self.tick,
            'tokens_by_altitude': tokens_by_altitude,
            'total_tokens': len(self.grid.all_tokens),
            'chains': [
                {
                    'id': chain.chain_id,
                    'code': chain.get_code_string(),
                    'length': len(chain.tokens),
                    'valid': chain.is_valid
                }
                for chain in self.biochemistry.chains
            ]
        }
