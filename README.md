# ML-Detection-Training-Tool

A comprehensive Unity package for training object detection agents using ML-Agents.

## Installation

1. Open the Unity Package Manager (Window > Package Manager)
2. Click the '+' button and select "Add package from disk"
3. Navigate to and select the `package.json` file from this package

## Quick Start

1. Open the ML Detection Configuration window (Window > ML Detection > Training Configuration)
2. Configure your environment and agent settings
3. Click "Create Training Environment" to set up your scene
4. Press Play to start training

## Features

- Multiple sensor types for object detection
- Curriculum learning support
- Real-time visualization and analysis
- Configurable training scenarios
- Advanced network architecture options
- Comprehensive data collection and analysis

## Configuration

### Environment Settings
- Environment Size: Define the size of each training area
- Obstacle Prefabs: Add objects that will be randomly placed
- Materials: Define materials for environment randomization

### Agent Settings
- Number of Agents: How many agents to train simultaneously
- Agent Prefab: The ML-Agent prefab with detection capabilities
- Minimum Spacing: Required space between agents

### Training Settings
- Curriculum Levels: Progressive difficulty settings
- Episode Length: Maximum duration of each training episode
- Progression Threshold: When to advance curriculum

### Visualization Settings
- Enable real-time visualization
- Configure update intervals
- Customize graph appearances

## Scripts

### Core Components
- `TrainingEnvironmentManager.cs`: Main training orchestrator
- `ObjectDetectionAgent.cs`: ML-Agent implementation
- `NetworkExperimentManager.cs`: Neural network configuration
- `VisualizationAnalysisManager.cs`: Real-time analysis tools

### Training Scenarios
- Search and Rescue
- Security Patrol
- Custom scenario support

## Data Collection

Training data is saved to:
```
Assets/MLDetection/Data/
├── TrainingStats/
├── NetworkConfigs/
└── AnalysisResults/
```

## Extending the Package

### Custom Scenarios
1. Create a new class inheriting from `TrainingScenario`
2. Implement required methods
3. Add to training environment

### Custom Network Architectures
1. Define architecture in `NetworkArchitecture` class
2. Configure through the editor
3. Add to experiments

## Support

For issues and feature requests, please contact:
[kushagranigam550@gmail.com](mailto:kushagranigam550@gmail.com)

## License

MIT License - See LICENSE.md for details
