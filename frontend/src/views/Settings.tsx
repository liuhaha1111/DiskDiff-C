import React from 'react';
import { Settings as SettingsIcon, Save, RotateCcw } from 'lucide-react';

export const Settings: React.FC = () => {
  return (
    <div className="h-full bg-white overflow-auto p-6">
      <div className="max-w-2xl mx-auto">
        <div className="flex items-center space-x-2 mb-6 pb-2 border-b border-border-strong">
          <SettingsIcon size={20} className="text-gray-700" />
          <h2 className="text-lg font-semibold text-gray-800">设置</h2>
        </div>

        <div className="space-y-6">
          <section>
            <h3 className="text-sm font-semibold text-gray-700 mb-3">常规</h3>
            <div className="space-y-3">
              <div className="flex items-center justify-between">
                <label className="text-xs text-gray-600">默认扫描盘符</label>
                <select className="border border-border-strong rounded-sm px-2 py-1 text-xs w-48 focus:border-blue-500 outline-none">
                  <option>C: [Windows]</option>
                  <option>D: [Data]</option>
                  <option>记住上次选择</option>
                </select>
              </div>
              <div className="flex items-center justify-between">
                <label className="text-xs text-gray-600">启动时行为</label>
                <select className="border border-border-strong rounded-sm px-2 py-1 text-xs w-48 focus:border-blue-500 outline-none">
                  <option>无动作</option>
                  <option>自动扫描默认盘符</option>
                </select>
              </div>
            </div>
          </section>

          <section>
            <h3 className="text-sm font-semibold text-gray-700 mb-3">自动快照</h3>
            <div className="space-y-3 bg-gray-50 p-3 border border-border-subtle rounded-sm">
              <label className="flex items-center space-x-2 cursor-pointer">
                <input type="checkbox" defaultChecked className="rounded border-gray-300 text-blue-600 focus:ring-blue-500" />
                <span className="text-xs font-medium text-gray-800">启用每日自动快照</span>
              </label>
              
              <div className="pl-5 space-y-3 mt-2">
                <div className="flex items-center justify-between">
                  <label className="text-xs text-gray-600">自动快照时间</label>
                  <input type="time" defaultValue="02:00" className="border border-border-strong rounded-sm px-2 py-1 text-xs w-32 focus:border-blue-500 outline-none" />
                </div>
                <div className="flex items-center justify-between">
                  <label className="text-xs text-gray-600">快照保留天数</label>
                  <div className="flex items-center space-x-2">
                    <input type="number" defaultValue="30" className="border border-border-strong rounded-sm px-2 py-1 text-xs w-20 text-right focus:border-blue-500 outline-none" />
                    <span className="text-xs text-gray-500">天</span>
                  </div>
                </div>
              </div>
            </div>
            <p className="text-[11px] text-gray-500 mt-2 flex items-center">
              <span className="text-amber-600 font-semibold mr-1">注意:</span> 第一版仅提供分析和快照对比功能，不会自动删除任何文件。
            </p>
          </section>

          <section>
            <h3 className="text-sm font-semibold text-gray-700 mb-3">高级</h3>
            <div className="space-y-3">
              <div className="flex items-center justify-between">
                <label className="text-xs text-gray-600">扫描模式偏好</label>
                <select className="border border-border-strong rounded-sm px-2 py-1 text-xs w-48 focus:border-blue-500 outline-none">
                  <option>优先 NTFS Fast (推荐)</option>
                  <option>强制兼容模式 (慢)</option>
                </select>
              </div>
              <div className="flex items-center justify-between">
                <label className="text-xs text-gray-600">日志与错误记录</label>
                <button className="text-xs text-blue-600 hover:underline">打开日志文件夹</button>
              </div>
            </div>
          </section>
        </div>

        <div className="mt-8 pt-4 border-t border-border-strong flex justify-end space-x-3">
          <button className="flex items-center space-x-1 px-4 py-1.5 border border-border-strong bg-white rounded-sm hover:bg-gray-50 text-xs">
            <RotateCcw size={14} />
            <span>恢复默认</span>
          </button>
          <button className="flex items-center space-x-1 px-4 py-1.5 bg-blue-600 text-white rounded-sm hover:bg-blue-700 text-xs shadow-sm">
            <Save size={14} />
            <span>保存设置</span>
          </button>
        </div>
      </div>
    </div>
  );
};
