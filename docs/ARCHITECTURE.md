# Digital Biochemical Simulator - Architecture Document

## Overview

This document maps the implementation classes to the design specification sections.

## Project Structure

```
src/DigitalBiochemicalSimulator/
├── Core/                       # Core domain entities
│   ├── TokenType.cs           # Section 3.1.2: Token type enumeration
│   ├── BondType.cs            # Section 3.4.1: Bond strength categories
│   ├── BondLocation.cs        # Section 3.1.3: Bond site locations
│   ├── TokenMetadata.cs       # Section 3.1.1: Token metadata structure
│   ├── BondSite.cs            # Section 3.1.3: Bond site structure
│   ├── Token.cs               # Section 3.1.1: Token entity
│   └── TokenChain.cs          # Section 4.3: Token chain management
│
├── DataStructures/             # Spatial data structures
│   ├── Vector3Int.cs          # 3D integer position
│   ├── Cell.cs                # Section 3.2.1: Grid cell structure
│   └── Grid.cs                # Section 3.2: 3D grid system
│
├── Simulation/                 # Simulation management
│   └── SimulationConfig.cs    # Section 5.1: Configuration parameters
│
├── Physics/                    # Physics systems (Phase 2)
│   ├── EnergyManager.cs       # Section 3.3.1: Energy dynamics
│   ├── MotionController.cs    # Section 3.3.3: Token movement
│   ├── GravitySimulator.cs    # Section 3.3.2: Gravity system
│   └── CollisionDetector.cs   # Section 3.3.1: Collision handling
│
├── Grammar/                    # Grammar and validation (Phase 3)
│   ├── GrammarRule.cs         # Section 3.5.1: Grammar definitions
│   ├── TokenPattern.cs        # Section 3.5.1: Pattern matching
│   ├── BondRulesEngine.cs     # Section 3.5: Bond compatibility
│   └── ASTValidator.cs        # Section 3.5.3: AST validation
│
├── Chemistry/                  # Bond chemistry (Phase 3)
│   ├── BondStrengthCalculator.cs  # Section 3.4.2: Electronegativity
│   ├── RepulsionHandler.cs    # Section 3.4.4: Token repulsion
│   └── ChainRegistry.cs       # Chain tracking
│
├── Damage/                     # Damage system (Phase 4)
│   ├── DamageSystem.cs        # Section 3.6: Damage mechanics
│   └── MetadataCorruptor.cs   # Section 3.6.2: Corruption types
│
└── Utilities/                  # Helper utilities
    ├── TokenPool.cs           # Object pooling
    └── Random.cs              # Random number generation

```

## Phase 1 Implementation Status ✅

### Completed Components

1. **Core Data Structures**
   - ✅ Vector3Int (3D position handling)
   - ✅ TokenType enumeration (all token types from spec)
   - ✅ BondType enumeration (Covalent, Ionic, Van der Waals)
   - ✅ BondLocation enumeration
   - ✅ TokenMetadata class
   - ✅ BondSite class
   - ✅ Token class
   - ✅ TokenChain class

2. **Grid System**
   - ✅ Cell class with capacity management
   - ✅ Grid class with 3D array
   - ✅ AddToken/RemoveToken methods
   - ✅ GetNeighbors (8 horizontal + 1 below)
   - ✅ Active cell tracking
   - ✅ Mutation zone marking

3. **Configuration**
   - ✅ SimulationConfig with all parameters
   - ✅ Preset configurations (Minimal, Standard, Complex, etc.)
   - ✅ Configuration validation

## Design Specification Mapping

| Specification Section | Implementation | Status |
|----------------------|----------------|---------|
| 3.1.1 Token Structure | Core/Token.cs | ✅ Complete |
| 3.1.2 Token Types | Core/TokenType.cs | ✅ Complete |
| 3.1.3 Bond Site Structure | Core/BondSite.cs | ✅ Complete |
| 3.2.1 Grid Structure | DataStructures/Grid.cs | ✅ Complete |
| 3.2.2 Cell Operations | DataStructures/Cell.cs | ✅ Complete |
| 3.3 Physics System | Physics/* | 🔄 Phase 2 |
| 3.4 Bond Chemistry | Chemistry/* | 🔄 Phase 3 |
| 3.5 Grammar System | Grammar/* | 🔄 Phase 3 |
| 3.6 Damage System | Damage/* | 🔄 Phase 4 |
| 4.3 Chain Management | Core/TokenChain.cs | ✅ Complete |
| 5.1 Configuration | Simulation/SimulationConfig.cs | ✅ Complete |
| 5.3 Presets | SimulationPresets | ✅ Complete |

## Key Design Decisions

### 1. C# + .NET 10.0
- **Rationale**: Cross-platform compatibility, strong typing, excellent performance
- **Future**: Can integrate with Unity for visualization or stay standalone

### 2. Grid-Based Spatial System
- **Structure**: 3D array of cells for O(1) position lookup
- **Active Cell Tracking**: HashSet to skip empty cells
- **Overflow Handling**: Redistribution to lowest-mass neighbors

### 3. Token Entity Design
- **Identity**: GUID for unique tracking
- **Physical Properties**: Position, mass, energy, velocity
- **Bonding**: Multiple bond sites per token
- **Metadata**: Separate class for damage-susceptible properties

### 4. Configuration Management
- **Presets**: Pre-configured scenarios for different experiments
- **Validation**: Built-in parameter validation
- **Cloning**: Support for configuration variants

## Next Steps (Phase 2)

1. **Physics System**
   - EnergyManager: Token energy management
   - MotionController: Rising/falling behavior
   - GravitySimulator: Downward movement
   - CollisionDetector: Token collisions

2. **Thermal Vent System**
   - ThermalVent: Token generation
   - TokenFactory: Weighted random token creation
   - Multiple vent support

3. **Simulation Engine**
   - TickManager: Time step control
   - Main simulation loop
   - Event queue

## Technology Stack

- **Language**: C# 10
- **Framework**: .NET 10.0
- **Serialization**: Newtonsoft.Json
- **Future Visualization**: Unity or MonoGame
- **Target Platform**: Windows (with Linux/Mac support)

## Performance Considerations

### Implemented
- Active cell tracking (skip empty cells)
- Object pooling preparation (TokenPool placeholder)
- Efficient spatial queries (grid-based)

### Planned
- Spatial indexing (Octree) for radius queries
- Parallel processing for token updates
- Event-driven bonding checks
- Lazy AST validation

## Testing Strategy

### Phase 1 Tests
- ✅ Vector3Int operations
- ✅ Grid creation and management
- ✅ Token creation and properties
- ✅ Cell capacity checking
- ✅ Neighbor finding
- ✅ Chain creation

### Phase 2 Tests (Planned)
- Token physics (rising/falling)
- Energy depletion
- Collision detection
- Gravity application

## Version History

- **v0.1.0** (Current): Phase 1 - Core data structures complete
- **v0.2.0** (Planned): Phase 2 - Physics and simulation engine
- **v0.3.0** (Planned): Phase 3 - Grammar and bonding systems
- **v0.4.0** (Planned): Phase 4 - Damage and mutation
- **v1.0.0** (Planned): Full simulation with visualization

---

**Last Updated**: 2025-11-19
**Status**: Phase 1 Complete ✅
