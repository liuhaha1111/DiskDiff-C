import { FileNode } from './types';

export const mockRootNode: FileNode = {
  id: 'root',
  name: 'C:',
  path: 'C:\\',
  type: 'folder',
  size: 415982346240,
  allocatedSize: 416000000000,
  diffSize: 19542130688,
  diffType: 'larger',
  modifiedAt: '2026-03-20 10:23:11',
  attributes: 'D',
  isExpanded: true,
  children: [
    {
      id: 'windows',
      name: 'Windows',
      path: 'C:\\Windows',
      type: 'folder',
      size: 32212254720,
      allocatedSize: 32250000000,
      diffSize: 536870912,
      diffType: 'larger',
      modifiedAt: '2026-03-20 09:15:00',
      attributes: 'D',
      children: []
    },
    {
      id: 'users',
      name: 'Users',
      path: 'C:\\Users',
      type: 'folder',
      size: 150323855360,
      allocatedSize: 150500000000,
      diffSize: 12884901888,
      diffType: 'larger',
      modifiedAt: '2026-03-20 11:05:22',
      attributes: 'D',
      children: [
        {
          id: 'users-admin',
          name: 'Administrator',
          path: 'C:\\Users\\Administrator',
          type: 'folder',
          size: 120000000000,
          allocatedSize: 120100000000,
          diffSize: 10000000000,
          diffType: 'larger',
          modifiedAt: '2026-03-20 11:05:22',
          attributes: 'D',
        }
      ]
    },
    {
      id: 'program-files',
      name: 'Program Files',
      path: 'C:\\Program Files',
      type: 'folder',
      size: 85899345920,
      allocatedSize: 86000000000,
      diffSize: 0,
      diffType: 'none',
      modifiedAt: '2026-03-15 14:22:10',
      attributes: 'D',
      children: []
    },
    {
      id: 'pagefile',
      name: 'pagefile.sys',
      path: 'C:\\pagefile.sys',
      type: 'file',
      size: 17179869184,
      allocatedSize: 17179869184,
      diffSize: 0,
      diffType: 'none',
      modifiedAt: '2026-03-20 08:00:00',
      attributes: 'HS',
    },
    {
      id: 'hiberfil',
      name: 'hiberfil.sys',
      path: 'C:\\hiberfil.sys',
      type: 'file',
      size: 12884901888,
      allocatedSize: 12884901888,
      diffSize: 0,
      diffType: 'none',
      modifiedAt: '2026-03-20 08:00:00',
      attributes: 'HS',
    },
    {
      id: 'temp',
      name: 'Temp',
      path: 'C:\\Temp',
      type: 'folder',
      size: 6549825126,
      allocatedSize: 6550000000,
      diffSize: 6549825126,
      diffType: 'new',
      modifiedAt: '2026-03-20 10:11:23',
      attributes: 'D',
    },
    {
      id: 'old-logs',
      name: 'OldLogs',
      path: 'C:\\OldLogs',
      type: 'folder',
      size: 0,
      allocatedSize: 0,
      diffSize: -1503238553,
      diffType: 'deleted',
      modifiedAt: '2026-03-19 23:59:59',
      attributes: 'D',
    }
  ]
};

export const formatBytes = (bytes: number, decimals = 2) => {
  if (bytes === 0) return '0 Bytes';
  const k = 1024;
  const dm = decimals < 0 ? 0 : decimals;
  const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB', 'PB', 'EB', 'ZB', 'YB'];
  const i = Math.floor(Math.log(Math.abs(bytes)) / Math.log(k));
  return parseFloat((Math.abs(bytes) / Math.pow(k, i)).toFixed(dm)) + ' ' + sizes[i];
};

export const formatDiff = (bytes: number) => {
  if (bytes === 0) return '-';
  const sign = bytes > 0 ? '+' : '-';
  return `${sign}${formatBytes(Math.abs(bytes))}`;
};
