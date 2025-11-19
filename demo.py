#!/usr/bin/env python3
"""
Quick demo of the 3D Code Biochemistry Simulation.
Runs a short simulation and generates visualizations.
"""

from simulation import Simulation
from visualizer import Visualizer
import os

def main():
    print("="*60)
    print("3D CODE BIOCHEMISTRY SIMULATION - DEMO")
    print("="*60)

    # Create a smaller simulation for demonstration
    print("\nInitializing simulation (30x30x30 grid)...")
    sim = Simulation(grid_size=30)

    # Run for 500 ticks
    print("Running simulation for 500 ticks...\n")
    sim.run(num_ticks=500, verbose=True)

    # Create visualizer
    viz = Visualizer(sim)

    # Print top chains
    print("\n" + "="*60)
    viz.print_chains(max_chains=15)

    # Generate visualizations
    print("\n" + "="*60)
    print("Generating visualizations...")
    os.makedirs('demo_output', exist_ok=True)

    # Create various plots
    print("  - Altitude distribution...")
    viz.plot_altitude_distribution(save_path='demo_output/altitude.png')

    print("  - Statistics over time...")
    viz.plot_statistics_over_time(save_path='demo_output/statistics.png')

    print("  - Horizontal slices...")
    for z in [5, 10, 15, 20]:
        viz.plot_horizontal_slice(z_level=z, save_path=f'demo_output/slice_z{z}.png')

    print("  - 3D scatter plot...")
    viz.create_3d_scatter(save_path='demo_output/3d_view.png')

    print("\nDemo complete! Check the 'demo_output' directory for visualizations.")
    print("\nKey observations:")
    print(f"  - Tokens spawned: {sim.vent.tokens_spawned}")
    print(f"  - Tokens remaining: {sim.grid.get_total_tokens()}")
    print(f"  - Chains formed: {len(sim.biochemistry.chains)}")
    print(f"  - Valid chains: {sum(1 for c in sim.biochemistry.chains if c.is_valid)}")

    if sim.biochemistry.chains:
        longest_chain = max(sim.biochemistry.chains, key=lambda c: len(c.tokens))
        print(f"  - Longest chain: {longest_chain.get_code_string()} ({len(longest_chain.tokens)} tokens)")

if __name__ == '__main__':
    main()
