import React from 'react';
import { AlertTriangle, CheckCircle, Info } from 'lucide-react';
import { type ScanState } from '../types';

interface StatusBarProps {
  state: ScanState;
}

export const StatusBar: React.FC<StatusBarProps> = ({ state }) => {
  return (
    <div className="flex items-center justify-between px-2 py-1 bg-panel-bg border-t border-border-strong text-[11px] text-gray-700">
      <div className="flex items-center space-x-4">
        <div className="flex items-center space-x-1">
          {state.status === 'scanning' ? (
            <div className="w-3 h-3 border-2 border-blue-500 border-t-transparent rounded-full animate-spin"></div>
          ) : state.status === 'partial' ? (
            <AlertTriangle size={12} className="text-amber-500" />
          ) : state.status === 'completed' ? (
            <CheckCircle size={12} className="text-green-500" />
          ) : (
            <Info size={12} className="text-gray-400" />
          )}
          <span>
            {state.status === 'scanning'
              ? `Scanning... ${state.currentPath}`
              : state.status === 'partial'
                ? 'Scan complete with limitations'
                : state.status === 'completed'
                  ? 'Scan complete'
                  : 'Ready'}
          </span>
        </div>

        {state.status === 'scanning' && (
          <div className="w-32 h-2.5 bg-gray-300 rounded-sm overflow-hidden border border-gray-400">
            <div className="h-full bg-blue-500 transition-all duration-200" style={{ width: `${state.progress}%` }}></div>
          </div>
        )}
      </div>

      <div className="flex items-center space-x-4">
        <span>Items: {state.scannedItems.toLocaleString()}</span>
        <span>Duration: {state.elapsedTime}</span>
        {state.errorCount > 0 && (
          <span className="text-red-600 flex items-center space-x-1">
            <AlertTriangle size={10} />
            <span>{state.errorCount} issues</span>
          </span>
        )}
      </div>
    </div>
  );
};
