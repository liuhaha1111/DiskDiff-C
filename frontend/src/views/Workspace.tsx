import React from 'react';
import { AlertTriangle, HardDrive, ArrowRight } from 'lucide-react';
import { DirectoryTree } from '../components/DirectoryTree';
import { DetailsTable } from '../components/DetailsTable';
import { TreeMap } from '../components/TreeMap';
import { getCurrentDirectory, getVisibleChildren } from '../workspace/session';
import { type FileNode, type ScanState, type WorkspaceDiffFilter, type WorkspaceSessionState } from '../types';

interface WorkspaceProps {
  data: FileNode;
  scanState: ScanState;
  onScan: () => void;
  sessionState: WorkspaceSessionState;
  onSelectNode: (nodeId: string) => void;
  onDiffFilterChange: (filter: WorkspaceDiffFilter) => void;
  onShowErrors: () => void;
}

export const Workspace: React.FC<WorkspaceProps> = ({
  data,
  scanState,
  onScan,
  sessionState,
  onSelectNode,
  onDiffFilterChange,
  onShowErrors,
}) => {
  const currentDirectory = getCurrentDirectory(data, sessionState.currentDirectoryId);
  const visibleItems = getVisibleChildren(data, sessionState);

  if (scanState.status === 'empty') {
    return (
      <div className="h-full flex flex-col items-center justify-center bg-white text-gray-600">
        <HardDrive size={64} className="text-gray-300 mb-4" />
        <h2 className="text-xl font-semibold text-gray-800 mb-2">DiskDiff Workspace Preview</h2>
        <p className="text-sm mb-6 max-w-md text-center">
          Scan a drive to capture a snapshot, compare it with the previous run, and inspect the changes through
          the tree, details table, and treemap.
        </p>
        <button
          onClick={onScan}
          className="flex items-center space-x-2 px-6 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 active:bg-blue-800 shadow-sm transition-colors"
        >
          <HardDrive size={18} />
          <span>Scan C: now</span>
          <ArrowRight size={18} />
        </button>
      </div>
    );
  }

  if (scanState.status === 'scanning') {
    return (
      <div className="h-full flex flex-col bg-white">
        <div className="flex-1 flex">
          <div className="w-[30%] border-r border-border-strong p-4">
            <div className="h-4 bg-gray-200 rounded w-1/2 mb-4 animate-pulse"></div>
            <div className="h-4 bg-gray-200 rounded w-3/4 mb-4 animate-pulse ml-4"></div>
            <div className="h-4 bg-gray-200 rounded w-2/3 mb-4 animate-pulse ml-4"></div>
            <div className="h-4 bg-gray-200 rounded w-1/2 mb-4 animate-pulse ml-8"></div>
          </div>
          <div className="w-[70%] flex flex-col">
            <div className="h-[60%] p-4">
              <div className="h-6 bg-gray-200 rounded w-full mb-2 animate-pulse"></div>
              <div className="h-6 bg-gray-200 rounded w-full mb-2 animate-pulse"></div>
              <div className="h-6 bg-gray-200 rounded w-full mb-2 animate-pulse"></div>
            </div>
            <div className="h-[40%] border-t border-border-strong p-4 flex gap-2">
              <div className="flex-1 bg-gray-200 rounded animate-pulse"></div>
              <div className="flex-1 bg-gray-200 rounded animate-pulse"></div>
            </div>
          </div>
        </div>
        <div className="absolute inset-0 bg-white/50 backdrop-blur-[1px] flex items-center justify-center z-20">
          <div className="bg-white border border-border-strong shadow-lg p-6 rounded flex flex-col items-center min-w-[320px]">
            <div className="w-8 h-8 border-4 border-blue-500 border-t-transparent rounded-full animate-spin mb-4"></div>
            <h3 className="font-semibold text-gray-800 mb-1">Scanning volume metadata...</h3>
            <p className="text-xs text-gray-500 mb-4">{scanState.currentPath}</p>
            <div className="w-full h-2 bg-gray-200 rounded-full overflow-hidden">
              <div className="h-full bg-blue-500" style={{ width: `${scanState.progress}%` }}></div>
            </div>
            <div className="w-full flex justify-between text-[10px] text-gray-500 mt-1">
              <span>{scanState.scannedItems.toLocaleString()} items</span>
              <span>{scanState.progress}%</span>
            </div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="h-full flex flex-col relative">
      {scanState.errorCount > 0 && (
        <div className="bg-amber-100 border-b border-amber-300 px-3 py-1.5 flex items-center justify-between text-amber-800 text-xs z-10">
          <div className="flex items-center space-x-2">
            <AlertTriangle size={14} />
            <span className="font-semibold">Scan completed with recoverable issues.</span>
            <span>The snapshot is usable, but some protected paths could not be read.</span>
          </div>
          <button onClick={onShowErrors} className="underline hover:text-amber-900">
            View {scanState.errorCount} scan errors
          </button>
        </div>
      )}

      {scanState.status === 'completed' && (
        <div className="bg-blue-50 border-b border-blue-200 px-3 py-1.5 flex items-center justify-between text-xs z-10">
          <div className="flex items-center space-x-4">
            <span className="font-semibold text-blue-800">Compared with snapshot from 2026-03-19 02:00</span>
            <div className="flex space-x-3">
              <span className="text-green-700">Added: 1,284</span>
              <span className="text-red-700">Deleted: 302</span>
              <span className="text-amber-700">Grown: 89</span>
              <span className="font-semibold text-gray-800">Net change: +18.2 GB</span>
            </div>
          </div>
        </div>
      )}

      <div className="flex-1 flex overflow-hidden">
        <div className="w-[30%] min-w-[220px] flex flex-col">
          <DirectoryTree
            data={data}
            currentDirectoryId={currentDirectory.id}
            onSelectNode={onSelectNode}
          />
        </div>

        <div className="splitter-v"></div>

        <div className="flex-1 flex flex-col min-w-[420px]">
          <div className="h-[60%] min-h-[220px]">
            <DetailsTable
              currentDirectory={currentDirectory}
              items={visibleItems}
              diffFilter={sessionState.diffFilter}
              focusedNodeId={sessionState.focusedNodeId}
              onDiffFilterChange={onDiffFilterChange}
              onSelectNode={onSelectNode}
            />
          </div>

          <div className="splitter-h"></div>

          <div className="h-[40%] min-h-[180px]">
            <TreeMap
              currentDirectory={currentDirectory}
              items={visibleItems}
              focusedNodeId={sessionState.focusedNodeId}
              onSelectNode={onSelectNode}
            />
          </div>
        </div>
      </div>
    </div>
  );
};
