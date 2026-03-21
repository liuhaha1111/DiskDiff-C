import React from 'react';
import { File, Folder } from 'lucide-react';
import { formatBytes, formatDiff } from '../mockData';
import { type FileNode, type WorkspaceDiffFilter } from '../types';

interface DetailsTableProps {
  currentDirectory: FileNode;
  items: FileNode[];
  diffFilter: WorkspaceDiffFilter;
  focusedNodeId: string | null;
  onDiffFilterChange: (filter: WorkspaceDiffFilter) => void;
  onSelectNode: (nodeId: string) => void;
}

const filters: WorkspaceDiffFilter[] = ['all', 'new', 'deleted', 'larger'];

export const DetailsTable: React.FC<DetailsTableProps> = ({
  currentDirectory,
  items,
  diffFilter,
  focusedNodeId,
  onDiffFilterChange,
  onSelectNode,
}) => {
  return (
    <div className="h-full flex flex-col bg-white">
      <div className="flex items-center justify-between px-2 py-1.5 bg-toolbar-bg border-b border-border-subtle text-xs space-x-2 gap-4">
        <span className="font-semibold text-gray-700 mr-2">Current directory: {currentDirectory.path}</span>
        <div className="flex bg-gray-200 rounded-sm p-0.5" aria-label="Change filters" role="toolbar">
          {filters.map((filter) => (
            <button
              key={filter}
              type="button"
              onClick={() => onDiffFilterChange(filter)}
              className={`px-2 py-0.5 rounded-sm ${diffFilter === filter ? 'bg-white shadow-sm font-medium' : 'text-gray-600 hover:bg-gray-300'}`}
            >
              {getFilterLabel(filter)}
            </button>
          ))}
        </div>
      </div>

      <div className="flex-1 overflow-auto">
        <table className="w-full text-left border-collapse min-w-max">
          <thead>
            <tr>
              <th className="table-header w-64">Name</th>
              <th className="table-header w-20">Type</th>
              <th className="table-header w-24 text-right">Allocated</th>
              <th className="table-header w-24 text-right">Delta</th>
              <th className="table-header w-20 text-center">Change</th>
              <th className="table-header w-36">Modified</th>
              <th className="table-header w-16">Attributes</th>
              <th className="table-header w-20 text-center">Action</th>
            </tr>
          </thead>
          <tbody>
            {items.length === 0 ? (
              <tr>
                <td colSpan={8} className="text-center py-8 text-gray-400 italic">
                  No items match the active search and filter.
                </td>
              </tr>
            ) : (
              items.map((item) => {
                const isSelected = focusedNodeId === item.id;

                return (
                  <tr
                    key={item.id}
                    aria-selected={isSelected}
                    className={`table-row cursor-pointer ${isSelected ? 'selected' : ''}`}
                    onClick={() => onSelectNode(item.id)}
                  >
                    <td className="table-cell flex items-center space-x-1.5">
                      {item.type === 'folder' ? (
                        <Folder size={14} className="text-amber-400 fill-amber-200 shrink-0" />
                      ) : (
                        <File size={14} className="text-gray-400 shrink-0" />
                      )}
                      <span className={`truncate ${item.diffType === 'deleted' ? 'line-through opacity-70' : ''}`}>
                        {item.name}
                      </span>
                    </td>
                    <td className="table-cell text-gray-600">{item.type === 'folder' ? 'Folder' : 'File'}</td>
                    <td className="table-cell text-right font-mono text-[11px]">{formatBytes(item.allocatedSize)}</td>
                    <td className={`table-cell text-right font-mono text-[11px] ${getDiffColor(item.diffType)}`}>
                      {formatDiff(item.diffSize)}
                    </td>
                    <td className="table-cell text-center">
                      <span className={`px-1.5 py-0.5 rounded-sm text-[10px] ${getChipClass(item.diffType)}`}>
                        {getDiffText(item.diffType)}
                      </span>
                    </td>
                    <td className="table-cell text-gray-600 font-mono text-[11px]">{item.modifiedAt}</td>
                    <td className="table-cell text-gray-500 font-mono text-[11px]">{item.attributes}</td>
                    <td className="table-cell text-center">
                      <button
                        type="button"
                        aria-label={`Reveal ${item.name} in Explorer`}
                        className="text-blue-600 hover:text-blue-800 hover:underline text-[11px]"
                        onClick={(event) => {
                          event.stopPropagation();
                          window.alert(`Reveal in Explorer: ${item.path}`);
                        }}
                      >
                        Reveal
                      </button>
                    </td>
                  </tr>
                );
              })
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
};

function getFilterLabel(filter: WorkspaceDiffFilter): string {
  switch (filter) {
    case 'all':
      return 'All';
    case 'new':
      return 'Added';
    case 'deleted':
      return 'Deleted';
    case 'larger':
      return 'Grown';
  }
}

function getDiffColor(type: string): string {
  switch (type) {
    case 'new':
      return 'text-diff-new';
    case 'deleted':
      return 'text-diff-del';
    case 'larger':
      return 'text-diff-grow';
    case 'smaller':
      return 'text-diff-shrink';
    default:
      return 'text-gray-500';
  }
}

function getDiffText(type: string): string {
  switch (type) {
    case 'new':
      return 'Added';
    case 'deleted':
      return 'Deleted';
    case 'larger':
      return 'Grown';
    case 'smaller':
      return 'Shrunk';
    default:
      return 'Unchanged';
  }
}

function getChipClass(type: string): string {
  switch (type) {
    case 'new':
      return 'bg-green-100 text-green-700';
    case 'deleted':
      return 'bg-red-100 text-red-700';
    case 'larger':
      return 'bg-amber-100 text-amber-700';
    case 'smaller':
      return 'bg-blue-100 text-blue-700';
    default:
      return 'bg-gray-100 text-gray-500';
  }
}
