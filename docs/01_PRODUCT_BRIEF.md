# SessionPad Product Brief

## One-line Description

SessionPad is a lightweight local note pad that appears beside the app window the user is currently working in.

## Problem

Developers and researchers often keep small pieces of working context scattered across terminals, editors, documents, TODO files, sticky notes, and memory.

The problem is not that users lack note-taking tools.

The problem is that notes are separated from the active work window.

When users return to a project, terminal session, document, or research task, they must remember where the relevant note is.

## Product Promise

SessionPad makes working context return with the window.

When a user returns to a VS Code, Cursor, Windsurf, Windows Terminal, PowerShell, browser, or document window, the relevant lightweight note should be easy to restore beside it.

## Target Users

Primary users:

- Developers
- Researchers
- Students
- Technical writers
- Power users who work across multiple windows

Initial MVP users:

- Developers using VS Code or Windows Terminal

## Core Use Cases

### IDE TODO

A developer opens a project in VS Code and attaches SessionPad.

They write:

```text
Pinned
- Do not modify generated runtime files.

Todo
- Reproduce failing test.
- Check git diff.
- Update edge case.

Commands
- pnpm test
- pnpm lint

Notes
- Auth mock returns undefined in the failing case.

When the developer returns to that work window, the note returns.

Terminal Commands

A developer uses Windows Terminal and stores repeat commands:

Commands
- pnpm test -- --runInBand
- git status
- git diff src/

Notes
- Last failure was caused by stale fixture data.

Clicking a command should eventually copy it to clipboard.

Positioning

SessionPad should feel like a tiny utility, not a workspace platform.

The product should be:

Fast
Local
Minimal
Window-aware
Non-invasive
Non-goals

SessionPad should not become:

A full note-taking app
A project management app
A markdown knowledge base
An AI coding assistant
A cloud document system
A dashboard
A chat app
A collaboration app
Privacy Principles

SessionPad must not automatically read or collect:

Screen contents
Terminal output
Editor contents
Browser contents
Files from the current project
Clipboard contents unless the user explicitly interacts with a command-copy feature

The MVP stores only user-entered notes in local files.