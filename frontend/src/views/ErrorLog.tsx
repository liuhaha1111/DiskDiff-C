import React from 'react';
import { AlertCircle, ArrowLeft, ExternalLink } from 'lucide-react';

interface ErrorLogProps {
  onBack: () => void;
}

export const ErrorLog: React.FC<ErrorLogProps> = ({ onBack }) => {
  const mockErrors = [
    { id: 1, path: 'C:\\System Volume Information', stage: 'Read MFT', error: 'Access denied (0x5)' },
    { id: 2, path: 'C:\\Windows\\System32\\config\\SAM', stage: 'File stat', error: 'File in use by another process (0x20)' },
    { id: 3, path: 'C:\\pagefile.sys', stage: 'Read attributes', error: 'Access denied (0x5)' },
  ];

  return (
    <div className="h-full flex flex-col bg-white">
      <div className="px-4 py-3 border-b border-border-strong flex items-center justify-between bg-toolbar-bg gap-4">
        <div className="flex items-center space-x-2">
          <AlertCircle size={18} className="text-red-600" />
          <h2 className="font-semibold text-gray-800">Scan Errors</h2>
        </div>
        <div className="flex items-center gap-3 text-xs">
          <button
            type="button"
            aria-label="Show workspace"
            onClick={onBack}
            className="inline-flex items-center gap-1 rounded-sm border border-border-strong bg-white px-2 py-1 hover:bg-gray-50"
          >
            <ArrowLeft size={14} />
            <span>Back to workspace</span>
          </button>
          <label className="flex items-center gap-2 text-gray-500">
            <span>Filter:</span>
            <select className="border border-border-strong rounded-sm px-2 py-1 bg-white outline-none">
              <option>All errors</option>
              <option>Access denied</option>
              <option>File in use</option>
            </select>
          </label>
        </div>
      </div>

      <div className="flex-1 overflow-auto p-4">
        <div className="border border-border-strong rounded-sm overflow-hidden">
          <table className="w-full text-left border-collapse">
            <thead>
              <tr className="bg-gray-100 border-b border-border-strong">
                <th className="py-1.5 px-3 text-xs font-semibold text-gray-700 w-1/2">Path</th>
                <th className="py-1.5 px-3 text-xs font-semibold text-gray-700 w-1/4">Stage</th>
                <th className="py-1.5 px-3 text-xs font-semibold text-gray-700 w-1/4">Error</th>
                <th className="py-1.5 px-3 text-xs font-semibold text-gray-700 w-16 text-center">Action</th>
              </tr>
            </thead>
            <tbody>
              {mockErrors.map((err, index) => (
                <tr
                  key={err.id}
                  className={`border-b border-border-subtle hover:bg-gray-50 ${index % 2 === 0 ? 'bg-white' : 'bg-gray-50/50'}`}
                >
                  <td className="py-1.5 px-3 text-xs font-mono text-gray-800 truncate max-w-[300px]" title={err.path}>
                    {err.path}
                  </td>
                  <td className="py-1.5 px-3 text-xs text-gray-600">{err.stage}</td>
                  <td className="py-1.5 px-3 text-xs text-red-600">{err.error}</td>
                  <td className="py-1.5 px-3 text-center">
                    <button
                      type="button"
                      aria-label={`Reveal ${err.path}`}
                      className="text-blue-600 hover:text-blue-800 p-1"
                      title="Reveal in Explorer"
                    >
                      <ExternalLink size={14} />
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <p className="text-[11px] text-gray-500 mt-4">
          Protected system files and folders can still fail during a scan without invalidating the rest of the snapshot.
          Surface the issue, keep the scan usable, and let the user inspect the failures afterwards.
        </p>
      </div>
    </div>
  );
};
