"""
Visualization tools for the 3D code biochemistry simulation.
Provides 2D slices, altitude plots, and chain visualizations.
"""

import matplotlib.pyplot as plt
import matplotlib.animation as animation
from matplotlib.gridspec import GridSpec
import numpy as np
from typing import Dict, List
from simulation import Simulation

class Visualizer:
    """Visualizes the simulation state."""

    def __init__(self, simulation: Simulation):
        self.simulation = simulation

    def plot_altitude_distribution(self, save_path: str = None):
        """Plot token distribution by altitude."""
        tokens = list(self.simulation.grid.all_tokens)
        if not tokens:
            print("No tokens to visualize.")
            return

        altitudes = [t.z for t in tokens]
        energies = [t.energy for t in tokens]
        damaged = [t.metadata['damaged'] for t in tokens]

        fig, (ax1, ax2) = plt.subplots(1, 2, figsize=(14, 6))

        # Altitude histogram
        ax1.hist(altitudes, bins=50, color='steelblue', alpha=0.7, edgecolor='black')
        ax1.set_xlabel('Altitude (z)')
        ax1.set_ylabel('Number of tokens')
        ax1.set_title(f'Token Distribution by Altitude (Tick {self.simulation.tick})')
        ax1.grid(True, alpha=0.3)

        # Altitude vs Energy scatter
        colors = ['red' if d else 'green' for d in damaged]
        ax2.scatter(altitudes, energies, c=colors, alpha=0.6, s=20)
        ax2.set_xlabel('Altitude (z)')
        ax2.set_ylabel('Energy')
        ax2.set_title('Token Energy vs Altitude')
        ax2.grid(True, alpha=0.3)

        # Add legend
        from matplotlib.patches import Patch
        legend_elements = [
            Patch(facecolor='green', alpha=0.6, label='Healthy'),
            Patch(facecolor='red', alpha=0.6, label='Damaged')
        ]
        ax2.legend(handles=legend_elements)

        plt.tight_layout()

        if save_path:
            plt.savefig(save_path, dpi=150, bbox_inches='tight')
            print(f"Saved altitude distribution to {save_path}")
        else:
            plt.show()

        plt.close()

    def plot_horizontal_slice(self, z_level: int, save_path: str = None):
        """Plot a horizontal slice of the grid at a specific altitude."""
        fig, ax = plt.subplots(figsize=(12, 12))

        # Get all tokens at this z level
        tokens_at_level = [
            t for t in self.simulation.grid.all_tokens
            if int(t.z) == z_level
        ]

        if not tokens_at_level:
            print(f"No tokens at altitude {z_level}")
            return

        # Plot tokens
        for token in tokens_at_level:
            color = 'red' if token.metadata['damaged'] else 'blue'
            size = token.mass * 10

            if token.chain_id is not None:
                marker = 's'  # Square for chained tokens
            else:
                marker = 'o'  # Circle for free tokens

            ax.scatter(token.x, token.y, c=color, s=size, marker=marker, alpha=0.6)

            # Optionally add token value as text
            if len(tokens_at_level) < 50:  # Only if not too crowded
                ax.text(token.x, token.y, token.value, fontsize=6, ha='center')

        ax.set_xlim(0, self.simulation.grid.size_x)
        ax.set_ylim(0, self.simulation.grid.size_y)
        ax.set_xlabel('X')
        ax.set_ylabel('Y')
        ax.set_title(f'Horizontal Slice at Z={z_level} (Tick {self.simulation.tick})')
        ax.grid(True, alpha=0.3)

        # Add legend
        from matplotlib.patches import Patch
        from matplotlib.lines import Line2D
        legend_elements = [
            Line2D([0], [0], marker='o', color='w', markerfacecolor='blue', markersize=8, label='Free token'),
            Line2D([0], [0], marker='s', color='w', markerfacecolor='blue', markersize=8, label='Chained token'),
            Patch(facecolor='red', alpha=0.6, label='Damaged')
        ]
        ax.legend(handles=legend_elements)

        plt.tight_layout()

        if save_path:
            plt.savefig(save_path, dpi=150, bbox_inches='tight')
            print(f"Saved horizontal slice to {save_path}")
        else:
            plt.show()

        plt.close()

    def plot_statistics_over_time(self, save_path: str = None):
        """Plot simulation statistics over time."""
        if not self.simulation.statistics_history:
            print("No statistics to plot.")
            return

        stats = self.simulation.statistics_history

        fig = plt.figure(figsize=(16, 10))
        gs = GridSpec(3, 2, figure=fig)

        # Token count over time
        ax1 = fig.add_subplot(gs[0, 0])
        ticks = [s['tick'] for s in stats]
        total_tokens = [s['total_tokens'] for s in stats]
        rising_tokens = [s['rising_tokens'] for s in stats]
        sinking_tokens = [s['sinking_tokens'] for s in stats]

        ax1.plot(ticks, total_tokens, label='Total', linewidth=2)
        ax1.plot(ticks, rising_tokens, label='Rising', alpha=0.7)
        ax1.plot(ticks, sinking_tokens, label='Sinking', alpha=0.7)
        ax1.set_xlabel('Tick')
        ax1.set_ylabel('Token Count')
        ax1.set_title('Token Population Over Time')
        ax1.legend()
        ax1.grid(True, alpha=0.3)

        # Energy over time
        ax2 = fig.add_subplot(gs[0, 1])
        total_energy = [s['total_energy'] for s in stats]
        ax2.plot(ticks, total_energy, color='orange', linewidth=2)
        ax2.set_xlabel('Tick')
        ax2.set_ylabel('Total Energy')
        ax2.set_title('Total Energy Over Time')
        ax2.grid(True, alpha=0.3)

        # Chain statistics
        ax3 = fig.add_subplot(gs[1, 0])
        total_chains = [s['total_chains'] for s in stats]
        valid_chains = [s['valid_chains'] for s in stats]
        ax3.plot(ticks, total_chains, label='Total chains', linewidth=2)
        ax3.plot(ticks, valid_chains, label='Valid chains', linewidth=2)
        ax3.set_xlabel('Tick')
        ax3.set_ylabel('Chain Count')
        ax3.set_title('Token Chains Over Time')
        ax3.legend()
        ax3.grid(True, alpha=0.3)

        # Chain length
        ax4 = fig.add_subplot(gs[1, 1])
        avg_length = [s['average_chain_length'] for s in stats]
        longest = [s['longest_chain'] for s in stats]
        ax4.plot(ticks, avg_length, label='Average length', linewidth=2)
        ax4.plot(ticks, longest, label='Longest chain', linewidth=2, alpha=0.7)
        ax4.set_xlabel('Tick')
        ax4.set_ylabel('Chain Length')
        ax4.set_title('Chain Length Over Time')
        ax4.legend()
        ax4.grid(True, alpha=0.3)

        # Damage over time
        ax5 = fig.add_subplot(gs[2, 0])
        damaged = [s['damaged_tokens'] for s in stats]
        ax5.plot(ticks, damaged, color='red', linewidth=2)
        ax5.set_xlabel('Tick')
        ax5.set_ylabel('Damaged Tokens')
        ax5.set_title('Token Damage Over Time')
        ax5.grid(True, alpha=0.3)

        # Average altitude
        ax6 = fig.add_subplot(gs[2, 1])
        avg_altitude = [s['average_altitude'] for s in stats]
        ax6.plot(ticks, avg_altitude, color='purple', linewidth=2)
        ax6.set_xlabel('Tick')
        ax6.set_ylabel('Average Altitude')
        ax6.set_title('Average Token Altitude Over Time')
        ax6.grid(True, alpha=0.3)

        plt.tight_layout()

        if save_path:
            plt.savefig(save_path, dpi=150, bbox_inches='tight')
            print(f"Saved statistics plot to {save_path}")
        else:
            plt.show()

        plt.close()

    def print_chains(self, max_chains: int = 10):
        """Print information about token chains."""
        chains = sorted(
            self.simulation.biochemistry.chains,
            key=lambda c: len(c.tokens),
            reverse=True
        )

        print(f"\n=== Top {max_chains} Token Chains ===")
        for i, chain in enumerate(chains[:max_chains]):
            status = "✓" if chain.is_valid else "✗"
            print(f"{i+1}. [{status}] {chain}")
            print(f"   Mass: {chain.get_total_mass()}, Energy: {chain.get_total_energy()}")

    def create_3d_scatter(self, save_path: str = None):
        """Create a 3D scatter plot of all tokens."""
        from mpl_toolkits.mplot3d import Axes3D

        tokens = list(self.simulation.grid.all_tokens)
        if not tokens:
            print("No tokens to visualize.")
            return

        fig = plt.figure(figsize=(12, 10))
        ax = fig.add_subplot(111, projection='3d')

        # Separate tokens by state
        free_tokens = [t for t in tokens if t.chain_id is None]
        chained_tokens = [t for t in tokens if t.chain_id is not None]

        # Plot free tokens
        if free_tokens:
            x = [t.x for t in free_tokens]
            y = [t.y for t in free_tokens]
            z = [t.z for t in free_tokens]
            colors = ['red' if t.metadata['damaged'] else 'blue' for t in free_tokens]
            ax.scatter(x, y, z, c=colors, marker='o', alpha=0.5, s=10, label='Free')

        # Plot chained tokens
        if chained_tokens:
            x = [t.x for t in chained_tokens]
            y = [t.y for t in chained_tokens]
            z = [t.z for t in chained_tokens]
            colors = ['orange' if t.metadata['damaged'] else 'green' for t in chained_tokens]
            ax.scatter(x, y, z, c=colors, marker='^', alpha=0.6, s=20, label='Chained')

        ax.set_xlabel('X')
        ax.set_ylabel('Y')
        ax.set_zlabel('Z (Altitude)')
        ax.set_title(f'3D Token Distribution (Tick {self.simulation.tick})')
        ax.legend()

        if save_path:
            plt.savefig(save_path, dpi=150, bbox_inches='tight')
            print(f"Saved 3D scatter plot to {save_path}")
        else:
            plt.show()

        plt.close()
