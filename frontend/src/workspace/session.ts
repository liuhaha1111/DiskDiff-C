import { type FileNode, type WorkspaceDiffFilter, type WorkspaceSessionState } from '../types';

export type { WorkspaceSessionState } from '../types';

export const defaultWorkspaceSessionState: WorkspaceSessionState = {
  currentDirectoryId: 'root',
  focusedNodeId: null,
  diffFilter: 'all',
  searchQuery: '',
};

export function findNodeById(root: FileNode, targetId: string | null | undefined): FileNode | null {
  if (!targetId) {
    return null;
  }

  if (root.id === targetId) {
    return root;
  }

  for (const child of root.children ?? []) {
    const match = findNodeById(child, targetId);
    if (match) {
      return match;
    }
  }

  return null;
}

export function getCurrentDirectory(root: FileNode, currentDirectoryId: string): FileNode {
  const candidate = findNodeById(root, currentDirectoryId);

  if (candidate?.type === 'folder') {
    return candidate;
  }

  return root;
}

export function getVisibleChildren(root: FileNode, state: WorkspaceSessionState): FileNode[] {
  const currentDirectory = getCurrentDirectory(root, state.currentDirectoryId);
  const normalizedQuery = state.searchQuery.trim().toLowerCase();

  return (currentDirectory.children ?? []).filter((item) => {
    return matchesDiffFilter(item, state.diffFilter) && matchesSearch(item, normalizedQuery);
  });
}

export function selectNodeInWorkspace(
  root: FileNode,
  state: WorkspaceSessionState,
  nodeId: string,
): WorkspaceSessionState {
  const node = findNodeById(root, nodeId);

  if (!node) {
    return state;
  }

  if (node.type === 'folder') {
    return {
      ...state,
      currentDirectoryId: node.id,
      focusedNodeId: node.id,
    };
  }

  return {
    ...state,
    focusedNodeId: node.id,
  };
}

export function reconcileWorkspaceState(
  root: FileNode,
  state: WorkspaceSessionState,
): WorkspaceSessionState {
  const currentDirectory = getCurrentDirectory(root, state.currentDirectoryId);
  const normalizedState = currentDirectory.id === state.currentDirectoryId
    ? state
    : {
        ...state,
        currentDirectoryId: currentDirectory.id,
      };

  if (!normalizedState.focusedNodeId || normalizedState.focusedNodeId === normalizedState.currentDirectoryId) {
    return normalizedState;
  }

  const visibleChildren = getVisibleChildren(root, normalizedState);
  const focusedStillVisible = visibleChildren.some((item) => item.id === normalizedState.focusedNodeId);

  if (focusedStillVisible) {
    return normalizedState;
  }

  return {
    ...normalizedState,
    focusedNodeId: null,
  };
}

function matchesDiffFilter(item: FileNode, filter: WorkspaceDiffFilter): boolean {
  return filter === 'all' ? true : item.diffType === filter;
}

function matchesSearch(item: FileNode, normalizedQuery: string): boolean {
  if (!normalizedQuery) {
    return true;
  }

  return item.name.toLowerCase().includes(normalizedQuery) || item.path.toLowerCase().includes(normalizedQuery);
}

