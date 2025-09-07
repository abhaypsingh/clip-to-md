# ClipTitle - Smart Clipboard to Markdown

[![Build and Release](https://github.com/abhaypsingh/clip-to-md/actions/workflows/build-and-release.yml/badge.svg)](https://github.com/abhaypsingh/clip-to-md/actions/workflows/build-and-release.yml)
[![GitHub release (latest by date)](https://img.shields.io/github/v/release/abhaypsingh/clip-to-md)](https://github.com/abhaypsingh/clip-to-md/releases)
[![GitHub license](https://img.shields.io/github/license/abhaypsingh/clip-to-md)](https://github.com/abhaypsingh/clip-to-md/blob/master/LICENSE)
[![.NET](https://img.shields.io/badge/.NET-9.0-blue)](https://dotnet.microsoft.com/download/dotnet/9.0)

A Windows system tray application that automatically converts clipboard content to Markdown files with intelligent naming based on content analysis.

## Features

- **System Tray Integration**: Runs quietly in the background with easy access from the system tray
- **Automatic Markdown Conversion**: Converts HTML and plain text clipboard content to clean Markdown
- **Smart File Naming**: Uses NLP-based content analysis to generate descriptive filenames
- **Content Analysis**: Automatically detects:
  - Content type (code, documentation, article, etc.)
  - Programming languages
  - Key topics and keywords
  - Domain context
- **Flexible Save Options**:
  - Create new files for each clip
  - Append to the last file
  - Ask each time
- **Rich Metadata**: Saves YAML front matter with content analysis results
- **Optional AI Integration**: Supports Ollama for enhanced title generation (disabled by default)

## Installation

1. Download the latest `ClipTitle.exe` from the [Releases](https://github.com/abhaypsingh/clip-to-md/releases) page
2. Run the executable - the app will start in your system tray
3. Right-click the tray icon to access settings

## Usage

1. **Copy content** to your clipboard (Ctrl+C)
2. The app automatically detects and processes the content
3. Based on your settings, it will:
   - Save to a new file with a smart name
   - Append to the last file
   - Show a dialog asking what to do

### Settings

Access settings by right-clicking the tray icon:

- **Save Directory**: Where Markdown files are saved
- **Auto-append**: Automatically append clips to the last file
- **Ask Every Time**: Show dialog for each clip
- **Ollama Integration**: Enable AI-powered title generation (optional)

## Smart Naming Examples

The app analyzes content to generate descriptive filenames:

- Code snippet → `20250107-143022-python-data-processing-script.md`
- Technical article → `20250107-143022-react-hooks-best-practices.md`
- General text → `20250107-143022-meeting-notes-project-update.md`

## File Format

Each saved file includes:
- YAML front matter with metadata
- Content type classification
- Detected programming language (for code)
- Keywords and topics
- Word and line counts
- Original markdown content

Example:
```yaml
---
title: "React Component State Management"
source: "clipboard"
created: "2025-01-07T14:30:22-05:00"
content_type: "code"
language: "javascript"
keywords: ["react", "usestate", "component", "hooks"]
word_count: 245
line_count: 32
---

[Your content here]
```

## Requirements

- Windows 10/11
- .NET 9.0 Runtime (included in release)

## Building from Source

1. Clone the repository
2. Open `ClipTitle.sln` in Visual Studio 2022 or later
3. Build in Release mode
4. The executable will be in `ClipTitle.WPF\bin\Release\net9.0-windows\`

## Technologies Used

- **WPF** (.NET 9.0) - UI framework
- **Hardcodet.NotifyIcon.Wpf** - System tray integration
- **ReverseMarkdown** - HTML to Markdown conversion
- **MVVM Pattern** with CommunityToolkit.Mvvm
- **Content Analysis** - Custom NLP implementation using .NET regex and pattern matching

## License

MIT License - See LICENSE file for details

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## Support

For issues or feature requests, please use the [GitHub Issues](https://github.com/abhaypsingh/clip-to-md/issues) page.

## CI/CD

This project uses GitHub Actions for continuous integration and deployment:
- Automatic builds on every push to master
- Semantic versioning based on commit messages
- Automatic release creation with portable and self-contained packages
- Dependency updates via Dependabot

See [CONTRIBUTING.md](CONTRIBUTING.md) for details on the development workflow.
