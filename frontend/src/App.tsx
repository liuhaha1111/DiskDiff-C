import React, { useEffect, useRef, useState } from 'react';
import { Toolbar } from './components/Toolbar';
import { StatusBar } from './components/StatusBar';
import { Workspace } from './views/Workspace';
import { Settings } from './views/Settings';
import { ErrorLog } from './views/ErrorLog';
import { mockRootNode } from './mockData';
import { type ScanState, type WorkspaceDiffFilter, type WorkspaceSessionState } from './types';
import {
  defaultWorkspaceSessionState,
  reconcileWorkspaceState,
  selectNodeInWorkspace,
} from './workspace/session';

type ViewType = 'workspace' | 'settings' | 'errors';

const completedScanState: ScanState = {
  status: 'completed',
  progress: 100,
  scannedItems: 1284302,
  elapsedTime: '00:00:12',
  errorCount: 3,
};

export default function App() {
  const [currentView, setCurrentView] = useState<ViewType>('workspace');
  const [scanState, setScanState] = useState<ScanState>(completedScanState);
  const [workspaceSession, setWorkspaceSession] = useState<WorkspaceSessionState>(
    defaultWorkspaceSessionState,
  );
  const scanTimerRef = useRef<number | null>(null);

  useEffect(() => {
    return () => {
      if (scanTimerRef.current !== null) {
        window.clearTimeout(scanTimerRef.current);
      }
    };
  }, []);

  const updateWorkspaceSession = (
    updater: WorkspaceSessionState | ((current: WorkspaceSessionState) => WorkspaceSessionState),
  ) => {
    setWorkspaceSession((current) => {
      const next = typeof updater === 'function' ? updater(current) : updater;
      return reconcileWorkspaceState(mockRootNode, next);
    });
  };

  const handleScan = () => {
    setCurrentView('workspace');
    setScanState({
      status: 'scanning',
      progress: 42,
      scannedItems: 542331,
      elapsedTime: '00:00:05',
      errorCount: 0,
      currentPath: 'C:\\Users\\Administrator\\AppData',
    });

    if (scanTimerRef.current !== null) {
      window.clearTimeout(scanTimerRef.current);
    }

    scanTimerRef.current = window.setTimeout(() => {
      setScanState(completedScanState);
    }, 900);
  };

  const handleSnapshot = () => {
    window.alert('Snapshot created from the current mock scan.');
  };

  const handleSelectNode = (nodeId: string) => {
    setCurrentView('workspace');
    updateWorkspaceSession((current) => selectNodeInWorkspace(mockRootNode, current, nodeId));
  };

  const handleSearchChange = (searchQuery: string) => {
    updateWorkspaceSession((current) => ({
      ...current,
      searchQuery,
    }));
  };

  const handleDiffFilterChange = (diffFilter: WorkspaceDiffFilter) => {
    updateWorkspaceSession((current) => ({
      ...current,
      diffFilter,
    }));
  };

  return (
    <div className="h-screen w-full flex flex-col bg-panel-bg overflow-hidden">
      <Toolbar
        currentView={currentView}
        onScan={handleScan}
        onSnapshot={handleSnapshot}
        onSettings={() => setCurrentView((current) => (current === 'settings' ? 'workspace' : 'settings'))}
        onErrors={() => setCurrentView('errors')}
        scanStatus={scanState.status}
        searchQuery={workspaceSession.searchQuery}
        onSearchChange={handleSearchChange}
      />

      <div className="flex-1 relative overflow-hidden">
        {currentView === 'workspace' && (
          <Workspace
            data={mockRootNode}
            scanState={scanState}
            onScan={handleScan}
            sessionState={workspaceSession}
            onSelectNode={handleSelectNode}
            onDiffFilterChange={handleDiffFilterChange}
            onShowErrors={() => setCurrentView('errors')}
          />
        )}
        {currentView === 'settings' && <Settings />}
        {currentView === 'errors' && <ErrorLog onBack={() => setCurrentView('workspace')} />}
      </div>

      <StatusBar state={scanState} />
    </div>
  );
}
