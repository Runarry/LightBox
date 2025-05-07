import { create } from 'zustand';

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
}

export const useUIStore = create<UIState>((set) => ({
  viewMode: 'card', // Default view mode
  setViewMode: (mode) => set({ viewMode: mode }),

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
          const stillOpen = useUIStore.getState().isModalOpen; // Check current state
          if (stillOpen && !action.onClick.toString().includes('hideModal') && !action.onClick.toString().includes('set({ isModalOpen: false })')) {
             set({ isModalOpen: false });
          }
        }
      }))
    }),
  hideModal: () => set({ isModalOpen: false, modalTitle: '', modalMessage: '', modalActions: [] }),
}));