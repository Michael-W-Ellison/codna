# Digital Biochemical Simulator - Test Suite

Comprehensive unit and integration tests for the Digital Biochemical Simulator.

## Test Organization

### Core Tests
- `TokenTests.cs` - Tests for Token class (creation, energy, damage, bonding)
- `GridTests.cs` - Tests for 3D grid system (add/remove, capacity, neighbors)

### Physics Tests
- `EnergyManagerTests.cs` - Tests for energy management system

### Chemistry Tests
- `BondStrengthCalculatorTests.cs` - Tests for bond strength calculations

### Damage Tests
- `DamageSystemTests.cs` - Tests for altitude-based damage and corruption

### Integration Tests
- `SimulationIntegrationTests.cs` - Complete simulation flow tests
- `ExpressionFormationTests.cs` - Tests for valid code expression formation (e.g., "5 + 3")

## Running Tests

```bash
# Run all tests
dotnet test

# Run specific test class
dotnet test --filter "FullyQualifiedName~TokenTests"

# Run with verbose output
dotnet test --logger "console;verbosity=detailed"
```

## Test Coverage

The test suite covers:

✅ Core data structures (Token, Grid, Cell, TokenChain)
✅ Physics systems (Energy management, motion, gravity)
✅ Chemistry systems (Bonding, strength calculation, repulsion)
✅ Damage systems (Altitude-based corruption, repair)
✅ Integration (Complete simulation flow)
✅ Expression formation (Grammar-based valid code structures)

## Key Test Scenarios

1. **Token Lifecycle**: Creation → Energy management → Damage → Destruction
2. **Grid Management**: Add/remove tokens, capacity limits, active cell tracking
3. **Bond Formation**: Grammar validation, strength calculation, energy costs
4. **Damage Accumulation**: Altitude-based rates, corruption types, critical damage
5. **Expression Formation**: Valid code structures like "5 + 3"
6. **Chain Stability**: 5-factor stability calculation with grammar validation
7. **Complete Simulation**: End-to-end integration of all systems

## Test Methodology

Tests follow **Test-Driven Development (TDD)** principles:
- **Arrange**: Set up test data and dependencies
- **Act**: Execute the system under test
- **Assert**: Verify expected outcomes

All tests are independent and can run in any order.
