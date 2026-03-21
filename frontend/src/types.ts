export type DiffType = 'none' | 'new' | 'deleted' | 'larger' | 'smaller';

export type WorkspaceDiffFilter = 'all' | 'new' | 'deleted' | 'larger';

export interface FileNode {
  id: string;
  name: string;
  path: string;
  type: 'file' | 'folder';
  size: number;
  allocatedSize: number;
  diffSize: number;
  diffType: DiffType;
  modifiedAt: string;
  attributes: string;
  children?: FileNode[];
  isExpanded?: boolean;
}

export interface WorkspaceSessionState {
  currentDirectoryId: string;
  focusedNodeId: string | null;
  diffFilter: WorkspaceDiffFilter;
  searchQuery: string;
}

export interface ScanState {
  status: 'empty' | 'scanning' | 'completed' | 'partial';
  progress: number;
  scannedItems: number;
  elapsedTime: string;
  errorCount: number;
  currentPath?: string;
}
