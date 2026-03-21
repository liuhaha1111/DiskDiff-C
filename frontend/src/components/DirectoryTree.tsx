import React, { useEffect, useState } from 'react';
import { ChevronRight, ChevronDown, Folder, HardDrive } from 'lucide-react';
import { formatBytes, formatDiff } from '../mockData';
import { type FileNode } from '../types';

interface DirectoryTreeProps {
  data: FileNode;
  currentDirectoryId: string;
  onSelectNode: (nodeId: string) => void;
}

interface TreeNodeProps {
  node: FileNode;
  level: number;
  currentDirectoryId: string;
  onSelectNode: (nodeId: string) => void;
}

const TreeNode: React.FC<TreeNodeProps> = ({ node, level, currentDirectoryId, onSelectNode }) => {
  const folderChildren = (node.children ?? []).filter((child) => child.type === 'folder');
  const hasChildren = folderChildren.length > 0;
  const [expanded, setExpanded] = useState(node.isExpanded ?? level === 0);
  const isSelected = currentDirectoryId === node.id;

  useEffect(() => {
    if (containsFolder(node, currentDirectoryId)) {
      setExpanded(true);
    }
  }, [currentDirectoryId, node]);

  return (
    <div role="none">
      <div
        role="treeitem"
        aria-level={level + 1}
        aria-expanded={hasChildren ? expanded : undefined}
        aria-selected={isSelected}
        tabIndex={0}
        className={`tree-item ${isSelected ? 'selected' : ''}`}
        style={{ paddingLeft: `${level * 16 + 4}px` }}
        onClick={() => onSelectNode(node.id)}
        onKeyDown={(event) => {
          if (event.key === 'Enter' || event.key === ' ') {
            event.preventDefault();
            onSelectNode(node.id);
          }
        }}
      >
        <button
          type="button"
          className="w-4 h-4 flex items-center justify-center mr-1 rounded-sm hover:bg-gray-200"
          aria-label={expanded ? `Collapse ${node.name}` : `Expand ${node.name}`}
          onClick={(event) => {
            event.stopPropagation();
            if (hasChildren) {
              setExpanded((current) => !current);
            }
          }}
        >
          {hasChildren ? (
            expanded ? <ChevronDown size={14} className="text-gray-500" /> : <ChevronRight size={14} className="text-gray-500" />
          ) : (
            <div className="w-4" />
          )}
        </button>

        <div className="mr-1.5">
          {node.id === 'root' ? (
            <HardDrive size={14} className="text-gray-600" />
          ) : (
            <Folder size={14} className="text-amber-400 fill-amber-200" />
          )}
        </div>

        <span className={`truncate flex-1 ${node.diffType === 'deleted' ? 'line-through opacity-70' : ''}`}>
          {node.name}
        </span>

        <div className="flex space-x-2 text-[11px] ml-2 font-mono">
          <span className="text-gray-600 w-16 text-right">{formatBytes(node.size, 1)}</span>
          <span className={`w-16 text-right ${getDiffColor(node.diffType)}`}>{formatDiff(node.diffSize)}</span>
        </div>
      </div>

      {expanded && hasChildren && (
        <div role="group">
          {folderChildren.map((child) => (
            <TreeNode
              key={child.id}
              node={child}
              level={level + 1}
              currentDirectoryId={currentDirectoryId}
              onSelectNode={onSelectNode}
            />
          ))}
        </div>
      )}
    </div>
  );
};

export const DirectoryTree: React.FC<DirectoryTreeProps> = ({ data, currentDirectoryId, onSelectNode }) => {
  return (
    <div className="h-full bg-white overflow-auto border-r border-border-strong">
      <div className="px-2 py-1 border-b border-border-subtle text-[11px] uppercase tracking-[0.18em] text-gray-500">
        Directory Tree
      </div>
      <div className="p-1 min-w-max" role="tree" aria-label="Directory tree">
        <TreeNode node={data} level={0} currentDirectoryId={currentDirectoryId} onSelectNode={onSelectNode} />
      </div>
    </div>
  );
};

function containsFolder(node: FileNode, targetId: string): boolean {
  if (node.id === targetId) {
    return true;
  }

  return (node.children ?? [])
    .filter((child) => child.type === 'folder')
    .some((child) => containsFolder(child, targetId));
}

function getDiffColor(type: string): string {
  switch (type) {
    case 'new':
      return 'text-diff-new';
    case 'deleted':
      return 'text-diff-del line-through opacity-70';
    case 'larger':
      return 'text-diff-grow';
    case 'smaller':
      return 'text-diff-shrink';
    default:
      return 'text-gray-500';
  }
}
