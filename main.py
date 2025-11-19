#!/usr/bin/env python3
"""
Main entry point for the 3D Code Biochemistry Simulation.

This simulation models programming language tokens as molecules in a 3D space,
where they rise, sink, combine according to grammar rules, and suffer damage
at high altitudes.
"""

import argparse
from simulation import Simulation
from visualizer import Visualizer
import os

def main():
    parser = argparse.ArgumentParser(
        description='3D Code Biochemistry Simulation'
    )
    parser.add_argument(
        '--ticks',
        type=int,
        default=1000,
        help='Number of simulation ticks to run (default: 1000)'
    )
    parser.add_argument(
        '--grid-size',
        type=int,
        default=100,
        help='Size of the 3D grid (default: 100x100x100)'
    )
    parser.add_argument(
        '--quiet',
        action='store_true',
        help='Run in quiet mode (no progress output)'
    )
    parser.add_argument(
        '--visualize',
        action='store_true',
        help='Generate visualizations after simulation'
    )
    parser.add_argument(
        '--output-dir',
        type=str,
        default='output',
        help='Directory for output files (default: output/)'
    )

    args = parser.parse_args()

    # Create output directory if needed
    if args.visualize:
        os.makedirs(args.output_dir, exist_ok=True)

    print("="*60)
    print("3D CODE BIOCHEMISTRY SIMULATION")
    print("="*60)
    print(f"\nGrid size: {args.grid_size}x{args.grid_size}x{args.grid_size}")
    print(f"Simulation duration: {args.ticks} ticks")
    print(f"Vent location: ({args.grid_size//2}, {args.grid_size//2}, 0)")
    print("\nInitializing simulation...")

    # Create simulation
    sim = Simulation(grid_size=args.grid_size)

    # Run simulation
    print(f"\nRunning simulation for {args.ticks} ticks...")
    sim.run(num_ticks=args.ticks, verbose=not args.quiet)

    # Print chain information
    if not args.quiet:
        viz = Visualizer(sim)
        viz.print_chains(max_chains=10)

    # Generate visualizations
    if args.visualize:
        print(f"\nGenerating visualizations in {args.output_dir}/...")
        viz = Visualizer(sim)

        # Altitude distribution
        print("  - Altitude distribution...")
        viz.plot_altitude_distribution(
            save_path=os.path.join(args.output_dir, 'altitude_distribution.png')
        )

        # Statistics over time
        print("  - Statistics over time...")
        viz.plot_statistics_over_time(
            save_path=os.path.join(args.output_dir, 'statistics.png')
        )

        # Horizontal slices at different altitudes
        for z in [10, 25, 50, 75]:
            print(f"  - Horizontal slice at z={z}...")
            viz.plot_horizontal_slice(
                z_level=z,
                save_path=os.path.join(args.output_dir, f'slice_z{z}.png')
            )

        # 3D scatter plot
        print("  - 3D scatter plot...")
        viz.create_3d_scatter(
            save_path=os.path.join(args.output_dir, '3d_scatter.png')
        )

        print(f"\nVisualizations saved to {args.output_dir}/")

    print("\nSimulation complete!")


if __name__ == '__main__':
    main()
