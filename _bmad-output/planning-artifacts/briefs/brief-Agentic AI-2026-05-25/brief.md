---
title: "BookTracker — Product Brief"
status: final
created: 2026-05-25
updated: 2026-05-25
---

# Product Brief: BookTracker

## Executive Summary

BookTracker is a full-stack web application for personal reading management, built on .NET (backend) and React (frontend) with a PostgreSQL database. Users catalog books by ISBN, track reading progress through a four-state lifecycle, and gain insight into their reading habits through a statistics dashboard powered by a lightweight event log.

The application is a complete, runnable demonstration of the BMad Method — from brainstorming through to a codebase hosted on GitHub. The scope is deliberately minimal: every feature that exists works completely, the code is well-structured and navigable, and nothing is included that does not serve a core user need.

## Who This Serves

**Primary user: developer following the BMad Method**
Reads regularly, wants to track progress and habits, and is building this application as a first-principles demonstration of structured product development. Success means the app runs, the features work, and the codebase tells a coherent architectural story that a PRD agent, developer agent, or human reviewer can pick up and continue.

**Secondary user: any individual reader**
Wants a clean, self-hosted reading tracker without social features or external platform dependency. Needs to add books quickly, see what they are currently reading, and get clear stats about their habits.

## The Problem

Readers who want to track what they are reading, how far they have progressed, and what their habits look like over time face an unsatisfying choice: heavyweight social platforms like Goodreads, which add noise, lock-in, and opinionated features, or manual spreadsheets that offer no UX, no computed stats, and no reading-state logic. Neither is a clean foundation for demonstrating modern full-stack application development patterns.

## The Solution

BookTracker gives a user four core capabilities:

1. **Catalog books by ISBN** — search by ISBN; if the book exists in the shared catalog it surfaces immediately. If not, Open Library is queried to prefill title, author, page count, and cover image. The user confirms and selects a genre from a predefined list.
2. **Track reading through a state machine** — each book on the user's shelf has one of four statuses: *Resting*, *Started*, *Finished*, or *Abandoned*. A context-aware action button always shows the valid next action. Re-reading a book creates a new reading record with an incrementing read count, preserving full history.
3. **Log page progress** — a numeric stepper input records how far the user has read. Each update is stored as a timestamped event capturing the previous and new values, enabling accurate period-based stats without complex queries.
4. **View personal statistics** — a stats strip is always visible on the bookshelf (total books, finished, in-progress, pages read this month). A dedicated stats page shows counts by status, pages read, books completed per period (last week / month / 3-, 6-, 9-, and 12-month windows), and a "your unfinished genre" insight identifying the genre with the highest started-but-not-finished ratio.

Authentication uses JWT bearer tokens. Registration collects email, password, first name, last name, and date of birth in a single form. Users' reading records are isolated; the book catalog (ISBN + metadata) is shared across all users.

## What Makes This Different

**Event log as stats engine.** Every meaningful user action (status change, page update) is recorded as a timestamped row with old and new values. Period stats are simple queries over this log — no precomputed counters, no nightly jobs. The design keeps analytics queryable and extensible.

**Shared catalog, personal records.** ISBN uniquely identifies a book. Metadata lives once; reading relationships are per-user. This separation avoids duplication and surfaces lightweight social proof: a `👥 X readers` count on each book card from a single COUNT query.

**Architectural clarity as a feature.** The backend follows a strict three-tier pattern: MVC controllers → service layer (interfaces + implementations) → repository layer (interfaces + implementations). The codebase is navigable by a developer or AI agent who has never seen it before.

## Scope

### In for v1
- User registration and login (JWT bearer tokens; email, password, first name, last name, date of birth)
- ISBN-based book search → Open Library prefill → shared catalog entry
- User bookshelf with colored status ribbons (Resting / Started / Finished / Abandoned)
- Four-state reading lifecycle with context-aware action button
- Page progress update via numeric stepper; progress popup shows Reading Journal (chronological action history)
- Re-read support: new `UserBook` record per reading attempt with `readingNumber` tracked
- `👥 X readers` count on book cards
- Stats strip always visible on bookshelf (total, finished, in-progress, pages this month)
- Full stats page: by-status counts, pages read, books per period, unfinished genre insight
- GitHub repository with local-run README

### Explicitly out of v1
- Profile edit page (name and date of birth set once at registration)
- Password confirmation field (show/hide toggle on single password input)
- Pagination on bookshelf (scrollable list)
- Title or author search (ISBN is the entry point)
- Reading streak, calendar heatmap, activity feed
- Email notifications
- Social features beyond reader count
- Cloud deployment (local run is the v1 delivery bar)
- Mobile-specific design (responsive layout is acceptable; native app is out)
- Custom date-range picker (fixed period buckets only)

## Success Criteria

- [ ] Application runs locally from a single `git clone` plus environment setup (PostgreSQL connection string, JWT secret)
- [ ] All core flows work end-to-end: register, login, add book via ISBN and Open Library, change reading status, update page progress, view stats
- [ ] `👥 X readers` count is visible on book cards
- [ ] Stats page shows counts by status and books per period for 30 / 90 / 180 / 365 day windows
- [ ] "Your unfinished genre" insight appears on the stats page when sufficient data exists
- [ ] Re-reading a finished or abandoned book creates a new reading record; prior record is preserved
- [ ] Codebase is in a public GitHub repository with a README covering local setup

## Architecture Notes

**Backend (.NET):**
- Full MVC controllers (no Minimal API)
- Three-tier: `Controllers` → `Services` (interface + implementation) → `Repositories` (interface + implementation)
- PostgreSQL via Entity Framework Core
- JWT bearer authentication middleware
- Namespace and folder structure should make each class easy to locate by type and domain

**Frontend (React):**
- Vite + TypeScript
- Communicates with backend via REST API
- State management: React Context (to confirm at architecture stage if a lightweight library is preferred)

**Key domain entities:**
- `Book` — shared catalog: ISBN, title, author, totalPages, genre, coverImageUrl
- `User` — email, passwordHash, firstName, lastName, dateOfBirth
- `UserBook` — userId, bookId, status, currentPages, readingNumber, startedAt, finishedAt
- `BookAction` — userId, userBookId, actionType, oldValue, newValue, timestamp

## Vision

Once the core experience and architecture are proven, natural extensions include: reading streak and calendar heatmap (the action log already holds the data), an ordered reading queue for the Resting shelf, genre filter tabs, CSV export, and a reading-pace estimator. The event log, shared catalog, and interface-based layering support these additions without structural rework.
