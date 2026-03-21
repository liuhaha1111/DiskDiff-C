# Frontend Workspace Interactions Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Turn the current `frontend/` prototype into a coherent workspace where tree navigation, details, treemap, search, diff filters, and error-log round trips all share one interaction model.

**Architecture:** Keep the mock `FileNode` dataset as the source of truth and move workspace session state up to `App`. Add pure helper functions for directory lookup, filtering, and focused-item reconciliation so the behavioral core is testable without rendering. Make `Toolbar`, `Workspace`, `DirectoryTree`, `DetailsTable`, and `TreeMap` controlled by that shared state instead of keeping disconnected local business state.

**Tech Stack:** React 19, TypeScript, Vite 6, Vitest, React Testing Library

---

### Task 1: Add a frontend test harness

**Files:**
- Modify: `frontend/package.json`
- Modify: `frontend/vite.config.ts`
- Modify: `frontend/tsconfig.json`
- Create: `frontend/src/test/setup.ts`
- Create: `frontend/src/test/vitest-env.d.ts`

**Step 1: Write the failing smoke test command target**

Add a `test` script and Vitest dependencies so the project can run browser-style component and helper tests:

```json
"scripts": {
  "test": "vitest run",
  "test:watch": "vitest"
}
```

**Step 2: Run the test command to verify it fails cleanly**

Run:

```bash
npm test
```

Expected: FAIL because no test files or Vitest configuration exist yet.

**Step 3: Add the minimal Vitest configuration**

Update Vite config to include:

```ts
test: {
  environment: 'jsdom',
  globals: true,
  setupFiles: './src/test/setup.ts',
}
```

Create `setup.ts` to load:

```ts
import '@testing-library/jest-dom/vitest';
```

**Step 4: Run the test command again**

Run:

```bash
npm test
```

Expected: PASS with zero or pending tests once the harness is in place.

### Task 2: Add workspace state helpers with tests

**Files:**
- Create: `frontend/src/workspace/session.ts`
- Create: `frontend/src/workspace/session.test.ts`
- Modify: `frontend/src/types.ts`

**Step 1: Write the failing tests**

Cover these helpers with pure tests:

```ts
it('returns a selected folder when the id resolves to a folder', () => {
  const result = getCurrentDirectory(mockRootNode, 'users');
  expect(result?.path).toBe('C:\\Users');
});

it('keeps the current directory when a file row is focused', () => {
  const next = createWorkspaceStateForNode(mockRootNode, 'pagefile');
  expect(next.currentDirectoryId).toBe('root');
  expect(next.focusedNodeId).toBe('pagefile');
});

it('filters direct children by diff type and search query', () => {
  const visible = getVisibleChildren(mockRootNode, {
    currentDirectoryId: 'root',
    focusedNodeId: null,
    diffFilter: 'new',
    searchQuery: 'temp',
  });

  expect(visible.map((item) => item.id)).toEqual(['temp']);
});

it('clears focus when the focused item is no longer visible', () => {
  const next = reconcileWorkspaceState(mockRootNode, {
    currentDirectoryId: 'root',
    focusedNodeId: 'pagefile',
    diffFilter: 'new',
    searchQuery: '',
  });

  expect(next.focusedNodeId).toBeNull();
});
```

**Step 2: Run the tests to verify they fail**

Run:

```bash
npm test -- src/workspace/session.test.ts
```

Expected: FAIL because the helpers do not exist yet.

**Step 3: Write the minimal implementation**

Implement:

- `WorkspaceDiffFilter`
- `WorkspaceSessionState`
- `findNodeById`
- `getCurrentDirectory`
- `getVisibleChildren`
- `selectNodeInWorkspace`
- `reconcileWorkspaceState`

The helper contract should treat folders as navigation targets and files as focus-only targets.

**Step 4: Run the tests again**

Run:

```bash
npm test -- src/workspace/session.test.ts
```

Expected: PASS.

### Task 3: Wire the shared state through the app shell

**Files:**
- Modify: `frontend/src/App.tsx`
- Modify: `frontend/src/components/Toolbar.tsx`
- Modify: `frontend/src/views/Workspace.tsx`
- Modify: `frontend/src/components/DirectoryTree.tsx`
- Modify: `frontend/src/components/DetailsTable.tsx`
- Modify: `frontend/src/components/TreeMap.tsx`
- Modify: `frontend/src/views/ErrorLog.tsx`

**Step 1: Write the failing integration tests**

Create:

- `frontend/src/App.test.tsx`

Cover:

```ts
it('keeps workspace state when switching to the error log and back', async () => {
  render(<App />);

  await user.click(screen.getByRole('button', { name: /show errors/i }));
  await user.click(screen.getByRole('button', { name: /show workspace/i }));

  expect(screen.getByDisplayValue('temp')).toBeInTheDocument();
});

it('updates the details table when a folder is chosen in the tree', async () => {
  render(<App />);
  await user.click(screen.getByRole('treeitem', { name: /users/i }));
  expect(screen.getByText('Administrator')).toBeInTheDocument();
});

it('keeps directory context and only changes focus when a file row is clicked', async () => {
  render(<App />);
  await user.click(screen.getByRole('row', { name: /pagefile\\.sys/i }));
  expect(screen.getByText('C:\\')).toBeInTheDocument();
});
```

**Step 2: Run the tests to verify they fail**

Run:

```bash
npm test -- src/App.test.tsx
```

Expected: FAIL because the components do not expose the shared interaction behavior yet.

**Step 3: Implement the minimal controlled UI**

Make these changes:

- `App` owns `workspaceSession` and passes controlled props into `Toolbar` and `Workspace`
- `Toolbar` exposes a controlled search box and view-toggle buttons with accessible labels
- `Workspace` derives current directory and visible children from helper functions
- `DirectoryTree` emits folder selection and marks the current directory row
- `DetailsTable` uses parent-controlled `diffFilter`, row selection, and empty states
- `TreeMap` renders the same filtered collection as the table and forwards clicks upward
- `ErrorLog` gets a callback to return to the workspace without resetting state

**Step 4: Run the integration tests again**

Run:

```bash
npm test -- src/App.test.tsx
```

Expected: PASS.

### Task 4: Verify the prototype end to end

**Files:**
- Modify: `frontend/src/App.tsx`
- Modify: `frontend/src/views/Workspace.tsx`
- Modify: `frontend/src/components/DetailsTable.tsx`
- Modify: `frontend/src/components/TreeMap.tsx`

**Step 1: Run the targeted test suite**

Run:

```bash
npm test -- src/workspace/session.test.ts src/App.test.tsx
```

Expected: PASS.

**Step 2: Run type-checking**

Run:

```bash
npm run lint
```

Expected: PASS.

**Step 3: Run the production build**

Run:

```bash
npm run build
```

Expected: PASS and emit the Vite production bundle.
