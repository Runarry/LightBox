import { h } from 'preact';
import { useUIStore } from '../stores/uiStore';
import './Modal.css'; // We'll create this CSS file next

export const Modal = () => {
  const { isModalOpen, modalTitle, modalMessage, modalActions, hideModal } = useUIStore();

  if (!isModalOpen) {
    return null;
  }

  return (
    <div class="modal-overlay" onClick={hideModal}> {/* Optional: click overlay to close */}
      <div class="modal-content" onClick={(e) => e.stopPropagation()}>
        {modalTitle && <div class="modal-header">
          <h2>{modalTitle}</h2>
        </div>}
        <div class="modal-body">
          {typeof modalMessage === 'string' ? <p>{modalMessage}</p> : modalMessage}
        </div>
        {modalActions && modalActions.length > 0 && (
          <div class="modal-footer">
            {modalActions.map((action, index) => (
              <button key={index} onClick={action.onClick} class={`modal-button ${action.className || ''}`}>
                {action.label}
              </button>
            ))}
          </div>
        )}
      </div>
    </div>
  );
};