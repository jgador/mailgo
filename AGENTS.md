# AGENTS.md
This file describes the released state of the master branch.
It is meant to prevent full repo rescans.
When you are not on master, also read AGENTS.delta.md.

## Product overview
Mailgo is a local first email campaign manager.
It has an ASP.NET Core API using SQLite and EF Core.
It has a React dashboard using Create React App and TypeScript.
It has an optional Electron desktop wrapper that bundles the frontend build and the backend together.

## Repo map
- backend
  - .NET solution, API and domain projects
- frontend
  - Create React App dashboard in frontend app
- desktop
  - Electron shell, packaging, and build scripts
- infra
  - docker compose for local runs
- docs
  - product docs, Electron docs
- data
  - local SQLite files for dev and compose runs
- scripts
  - helper scripts

## Common dev workflows
Web dev
- backend
  - run the app host project from backend
  - the default dev URL is localhost on port 8080 unless overridden by ASPNETCORE_URLS
- frontend
  - run npm start from frontend app
  - set REACT_APP_API_BASE_URL to point to the backend api base

Docker compose
- run docker compose from infra
- web runs on localhost 3000 and proxies api
- api runs on localhost 8080
- sqlite data is persisted in the data folder

Desktop dev
- run npm run dev from desktop
- it starts the Create React App dev server
- it starts dotnet watch for the backend
- it starts Electron pointed at the dev server

## Guardrails for coding agents
- do not store SMTP credentials or passwords in the repo
- prefer small, testable commits
- keep changes scoped to the requested task
- update docs when behavior, ports, or run steps change
- if a change affects ports or startup, update both docs and this file

## How to work on non master branches
- never treat this file as the full truth for your current branch
- always read AGENTS.delta.md first on non master branches
- use the delta file as the authoritative description of what changed since master

Recommended branch workflow for agents
- find the merge base with master
  - git merge-base HEAD origin master
- inspect changes without scanning the whole repo
  - git diff --stat MERGE_BASE..HEAD
  - git diff MERGE_BASE..HEAD
  - git log --oneline MERGE_BASE..HEAD
- only open files that appear in the diff unless you have a clear dependency reason
- after implementing changes, update AGENTS.delta.md to reflect new commits and file touch points


# AGENTS.delta.md
This file captures the moving state of the current branch compared to master.
It must be updated whenever new commits land on the branch.
Master is treated as stable, this file is treated as current truth for the branch.

## Delta header
- master ref, the master commit you branched from or last synced to
- merge base, output of git merge-base HEAD origin master
- branch head, output of git rev-parse HEAD
- generated on, local date time

Example
- master ref 0000000
- merge base 0000000
- branch head 0000000
- generated on 2025 12 12 14 00

## High level summary
- what changed and why
- what is still in progress
- what is risky or likely to break

## Areas touched
- backend
  - list key folders changed
  - list key endpoints, services, or DB migrations changed
- frontend
  - list key pages or components changed
- desktop
  - list packaging or port changes
- infra
  - list compose changes
- docs
  - list docs that need updating

## Behavior changes
- ports, URLs, env vars
- breaking changes
- new configuration

## Quick verification
- backend build command
- frontend build command
- any tests added or updated
- docker compose smoke check if relevant
- desktop smoke check if relevant

## Commit log since merge base
- include the output of git log --oneline MERGE_BASE..HEAD

## File list since merge base
- include the output of git diff --name-only MERGE_BASE..HEAD
