import { create } from 'zustand';
import i18n from '../i18n'; // 导入 i18next 实例
import type { Workspace, WorkspaceInfo } from '../services/lightboxApi'; // 引入工作区类型

export type ViewMode = 'card' | 'list';

interface ModalAction {
  label: string;
  onClick: () => void;
  className?: string; // e.g., 'primary', 'secondary', 'danger'
}

interface ModalState {
  isModalOpen: boolean;
  modalTitle: string;
  modalMessage: string | preact.ComponentChild; // Allow JSX in message
  modalActions: ModalAction[];
  showModal: (title: string, message: string | preact.ComponentChild, actions?: ModalAction[]) => void;
  hideModal: () => void;
}

interface UIState extends ModalState {
  viewMode: ViewMode;
  setViewMode: (mode: ViewMode) => void;
  // Potentially other UI states can be added here later, e.g., log window visibility

  // i18n state
  currentLanguage: string;
  supportedLanguages: { code: string; name: string }[];
  setLanguage: (language: string) => void;

  // Workspace Modal specific state
  isWorkspaceModalOpen: boolean;
  editingWorkspace: WorkspaceInfo | Workspace | null; // For pre-filling the form
  openWorkspaceModal: (workspace?: WorkspaceInfo | Workspace | null) => void;
  closeWorkspaceModal: () => void;

  // Workspace Panel visibility
  isWorkspacePanelOpen: boolean;
  toggleWorkspacePanel: () => void;
  openWorkspacePanel: () => void;
  closeWorkspacePanel: () => void;
}

export const useUIStore = create<UIState>((set, get) => ({
  viewMode: 'card', // Default view mode
  setViewMode: (mode) => set({ viewMode: mode }),

  // i18n state and actions
  currentLanguage: i18n.language || 'en', // 从 i18next 获取初始语言
  supportedLanguages: [ // 可以从配置或 API 获取
    { code: 'en', name: 'English' },
    { code: 'zh', name: '中文' },
  ],
  setLanguage: (language: string) => {
    i18n.changeLanguage(language).then(() => {
      set({ currentLanguage: language });
      // 可以选择将语言偏好保存到 localStorage
      localStorage.setItem('i18nextLng', language);
    });
  },

  // Modal state and actions
  isModalOpen: false,
  modalTitle: '',
  modalMessage: '',
  modalActions: [],
  showModal: (title, message, actions = [{ label: 'OK', onClick: () => set({ isModalOpen: false }) }]) =>
    set({
      isModalOpen: true,
      modalTitle: title,
      modalMessage: message,
      modalActions: actions.map(action => ({
        ...action,
        onClick: () => { // Ensure modal closes after action, unless action itself reopens or navigates
          action.onClick();
          // Check if the original onClick didn't already handle closing
          // This is a simple approach; more complex scenarios might need explicit close control
          const stillOpen = get().isModalOpen; // Check current state using get()
          if (stillOpen && !action.onClick.toString().includes('hideModal') && !action.onClick.toString().includes('set({ isModalOpen: false })')) {
             set({ isModalOpen: false });
          }
       }
     }))
   }),
  hideModal: () => set({ isModalOpen: false, modalTitle: '', modalMessage: '', modalActions: [] }),

  // Workspace Modal actions
  isWorkspaceModalOpen: false,
  editingWorkspace: null,
  openWorkspaceModal: (workspace = null) => set({ isWorkspaceModalOpen: true, editingWorkspace: workspace }),
  closeWorkspaceModal: () => set({ isWorkspaceModalOpen: false, editingWorkspace: null }),

  // Workspace Panel actions
  isWorkspacePanelOpen: false, // Default to closed
  toggleWorkspacePanel: () => set((state) => ({ isWorkspacePanelOpen: !state.isWorkspacePanelOpen })),
  openWorkspacePanel: () => set({ isWorkspacePanelOpen: true }),
  closeWorkspacePanel: () => set({ isWorkspacePanelOpen: false }),
}));

// 监听 i18next 的 languageChanged 事件，确保 store 与 i18next 状态同步
// 这在通过浏览器检测器或其他方式改变语言时尤其重要
i18n.on('languageChanged', (lng) => {
  useUIStore.setState({ currentLanguage: lng });
});
