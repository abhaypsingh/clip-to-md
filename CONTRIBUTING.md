# Contributing to ClipTitle

Thank you for your interest in contributing to ClipTitle! This document provides guidelines and instructions for contributing.

## How to Contribute

### Reporting Issues
- Use the [GitHub Issues](https://github.com/abhaypsingh/clip-to-md/issues) page
- Check if the issue already exists
- Include steps to reproduce the problem
- Provide system information (Windows version, .NET version)

### Suggesting Features
- Open a discussion in Issues with the `enhancement` label
- Describe the feature and its use case
- Consider how it fits with existing functionality

### Code Contributions

#### Development Setup
1. Fork the repository
2. Clone your fork: `git clone https://github.com/YOUR-USERNAME/clip-to-md.git`
3. Create a feature branch: `git checkout -b feature/your-feature-name`
4. Install .NET 9.0 SDK
5. Open the solution in Visual Studio 2022 or VS Code

#### Making Changes
1. Follow the existing code style
2. Add XML documentation for public APIs
3. Update README.md if adding features
4. Test your changes thoroughly

#### Commit Messages
Use semantic commit messages for automatic versioning:
- `fix:` for bug fixes (triggers patch version bump)
- `feat:` for new features (triggers minor version bump)
- `BREAKING CHANGE:` for breaking changes (triggers major version bump)
- `docs:` for documentation changes
- `style:` for code style changes
- `refactor:` for refactoring
- `test:` for test additions/changes
- `chore:` for maintenance tasks

Examples:
```
feat: add support for custom markdown templates
fix: resolve file locking issue when appending
docs: update installation instructions
```

For version control:
- `+semver: major` or `+semver: breaking` - Major version bump
- `+semver: minor` or `+semver: feature` - Minor version bump  
- `+semver: patch` or `+semver: fix` - Patch version bump
- `+semver: none` or `+semver: skip` - No version bump

#### Pull Request Process
1. Ensure your branch is up to date with master
2. Push your changes to your fork
3. Create a Pull Request with:
   - Clear title describing the change
   - Description of what and why
   - Link to related issues
4. Wait for CI checks to pass
5. Address review feedback

### CI/CD Pipeline

Our GitHub Actions workflow automatically:
- Builds the project on every push
- Runs tests (when available)
- Creates releases with semantic versioning
- Publishes portable and self-contained packages
- Updates dependencies via Dependabot

The version is automatically determined based on:
- Commit messages (conventional commits)
- GitVersion configuration
- Branch naming conventions

### Code Style Guidelines

- Use C# 12 features where appropriate
- Follow .NET naming conventions
- Keep methods small and focused
- Use dependency injection
- Write self-documenting code
- Add comments for complex logic

### Testing

- Add unit tests for new functionality
- Ensure existing tests pass
- Test on Windows 10 and 11
- Verify both portable and self-contained builds

## Questions?

Feel free to open an issue for any questions about contributing!