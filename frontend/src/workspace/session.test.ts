import { describe, expect, it } from 'vitest';
import { mockRootNode } from '../mockData';
import {
  getCurrentDirectory,
  getVisibleChildren,
  reconcileWorkspaceState,
  selectNodeInWorkspace,
  type WorkspaceSessionState,
} from './session';

const createState = (overrides: Partial<WorkspaceSessionState> = {}): WorkspaceSessionState => ({
  currentDirectoryId: 'root',
  focusedNodeId: null,
  diffFilter: 'all',
  searchQuery: '',
  ...overrides,
});

describe('workspace session helpers', () => {
  it('returns a selected folder when the id resolves to a folder', () => {
    const result = getCurrentDirectory(mockRootNode, 'users');

    expect(result?.path).toBe('C:\\Users');
  });

  it('keeps the current directory when a file row is focused', () => {
    const next = selectNodeInWorkspace(mockRootNode, createState(), 'pagefile');

    expect(next.currentDirectoryId).toBe('root');
    expect(next.focusedNodeId).toBe('pagefile');
  });

  it('moves into a folder when the selection is a folder node', () => {
    const next = selectNodeInWorkspace(mockRootNode, createState(), 'users');

    expect(next.currentDirectoryId).toBe('users');
    expect(next.focusedNodeId).toBe('users');
  });

  it('filters direct children by diff type and search query', () => {
    const visible = getVisibleChildren(
      mockRootNode,
      createState({
        diffFilter: 'new',
        searchQuery: 'temp',
      }),
    );

    expect(visible.map((item) => item.id)).toEqual(['temp']);
  });

  it('clears focus when the focused item is no longer visible', () => {
    const next = reconcileWorkspaceState(
      mockRootNode,
      createState({
        focusedNodeId: 'pagefile',
        diffFilter: 'new',
      }),
    );

    expect(next.focusedNodeId).toBeNull();
    expect(next.currentDirectoryId).toBe('root');
  });
});
