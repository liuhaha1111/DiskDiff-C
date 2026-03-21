import React from 'react';
import { formatBytes, formatDiff } from '../mockData';
import { type FileNode } from '../types';

interface TreeMapProps {
  currentDirectory: FileNode;
  items: FileNode[];
  focusedNodeId: string | null;
  onSelectNode: (nodeId: string) => void;
}

export const TreeMap: React.FC<TreeMapProps> = ({ currentDirectory, items, focusedNodeId, onSelectNode }) => {
  const displayItems = items.filter((item) => item.size > 0);
  const totalSize = displayItems.reduce((sum, item) => sum + item.size, 0);

  if (displayItems.length === 0) {
    return (
      <div className="h-full bg-white flex items-center justify-center text-gray-400 border-t border-border-strong">
        No treemap tiles are available for the current result set.
      </div>
    );
  }

  return (
    <div className="h-full bg-white border-t border-border-strong flex flex-col">
      <div className="px-2 py-1 bg-toolbar-bg border-b border-border-subtle text-xs font-semibold text-gray-600 flex justify-between">
        <span>Treemap</span>
        <span className="font-normal text-gray-500">{currentDirectory.path}</span>
      </div>
      <div className="flex-1 p-2 flex flex-wrap gap-2 content-start overflow-auto">
        {displayItems.map((item) => {
          const share = totalSize === 0 ? 0 : item.size / totalSize;
          const isSelected = focusedNodeId === item.id;

          return (
            <button
              key={item.id}
              type="button"
              aria-label={`Treemap item ${item.name}`}
              onClick={() => onSelectNode(item.id)}
              className={`border rounded p-2 text-left overflow-hidden transition-all hover:brightness-95 ${getBlockClass(item.diffType)} ${isSelected ? 'ring-2 ring-blue-500' : ''}`}
              style={{
                flex: `${Math.max(item.size, 1)} 1 180px`,
                minHeight: `${96 + Math.round(share * 140)}px`,
              }}
            >
              <div className="flex h-full flex-col justify-between">
                <div>
                  <div className="font-semibold truncate">{item.name}</div>
                  <div className="text-[11px] opacity-70">{item.type === 'folder' ? 'Folder' : 'File'}</div>
                </div>
                <div className="text-[11px] opacity-80 space-y-1">
                  <div>{formatBytes(item.size)}</div>
                  <div>{formatDiff(item.diffSize)}</div>
                </div>
              </div>
            </button>
          );
        })}
      </div>
    </div>
  );
};

function getBlockClass(type: string): string {
  switch (type) {
    case 'new':
      return 'bg-green-500/15 border-green-600/40';
    case 'deleted':
      return 'bg-red-500/10 border-red-600/30';
    case 'larger':
      return 'bg-amber-500/20 border-amber-600/40';
    case 'smaller':
      return 'bg-blue-500/15 border-blue-600/35';
    default:
      return 'bg-sky-500/10 border-sky-600/30';
  }
}
