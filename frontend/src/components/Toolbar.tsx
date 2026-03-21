import React from 'react';
import { Play, Camera, Settings, AlertCircle, Search, HardDrive, Clock } from 'lucide-react';

interface ToolbarProps {
  currentView: 'workspace' | 'settings' | 'errors';
  onScan: () => void;
  onSnapshot: () => void;
  onSettings: () => void;
  onErrors: () => void;
  scanStatus: string;
  searchQuery: string;
  onSearchChange: (value: string) => void;
}

export const Toolbar: React.FC<ToolbarProps> = ({
  currentView,
  onScan,
  onSnapshot,
  onSettings,
  onErrors,
  scanStatus,
  searchQuery,
  onSearchChange,
}) => {
  return (
    <div className="flex items-center justify-between px-2 py-2 bg-toolbar-bg border-b border-border-strong gap-4">
      <div className="flex items-center space-x-2 min-w-0">
        <div className="flex items-center space-x-1 mr-4 shrink-0">
          <HardDrive size={16} className="text-gray-600" />
          <span className="font-semibold text-sm">DiskDiff</span>
        </div>

        <select
          aria-label="Drive selector"
          className="border border-border-strong bg-white px-2 py-1 text-xs rounded-sm focus:outline-none focus:border-blue-500"
        >
          <option>C: [Windows] (NTFS)</option>
          <option>D: [Data] (NTFS)</option>
        </select>

        <button
          onClick={onScan}
          disabled={scanStatus === 'scanning'}
          className="flex items-center space-x-1 px-3 py-1 bg-white border border-border-strong rounded-sm hover:bg-gray-50 active:bg-gray-100 disabled:opacity-50"
        >
          <Play size={14} className="text-green-600" />
          <span>Scan Now</span>
        </button>

        <button
          onClick={onSnapshot}
          disabled={scanStatus === 'scanning'}
          className="flex items-center space-x-1 px-3 py-1 bg-white border border-border-strong rounded-sm hover:bg-gray-50 active:bg-gray-100 disabled:opacity-50"
        >
          <Camera size={14} className="text-blue-600" />
          <span>Create Snapshot</span>
        </button>

        <div className="h-4 w-px bg-border-strong mx-2"></div>

        <div className="flex items-center space-x-1 text-gray-500 text-xs shrink-0">
          <Clock size={12} />
          <span>Latest snapshot: 2026-03-20 02:00</span>
        </div>
        <div className="flex items-center space-x-1 text-gray-500 text-xs ml-2 shrink-0">
          <span className="px-1 bg-gray-200 rounded text-[10px]">Daily 02:00</span>
          <span className="px-1 bg-gray-200 rounded text-[10px]">NTFS Fast</span>
        </div>
      </div>

      <div className="flex items-center space-x-2 shrink-0">
        <div className="relative">
          <Search size={14} className="absolute left-2 top-1.5 text-gray-400" />
          <input
            aria-label="Search current directory"
            type="text"
            value={searchQuery}
            onChange={(event) => onSearchChange(event.target.value)}
            placeholder="Search current folder..."
            className="pl-7 pr-2 py-1 border border-border-strong bg-white text-xs rounded-sm w-52 focus:outline-none focus:border-blue-500"
          />
        </div>

        <button
          type="button"
          aria-label="Show errors"
          aria-pressed={currentView === 'errors'}
          onClick={onErrors}
          className={`p-1 rounded-sm ${currentView === 'errors' ? 'bg-red-100 text-red-700' : 'hover:bg-gray-200 text-gray-600'}`}
          title="Show errors"
        >
          <AlertCircle size={16} />
        </button>
        <button
          type="button"
          aria-label="Show settings"
          aria-pressed={currentView === 'settings'}
          onClick={onSettings}
          className={`p-1 rounded-sm ${currentView === 'settings' ? 'bg-blue-100 text-blue-700' : 'hover:bg-gray-200 text-gray-600'}`}
          title="Show settings"
        >
          <Settings size={16} />
        </button>
      </div>
    </div>
  );
};
