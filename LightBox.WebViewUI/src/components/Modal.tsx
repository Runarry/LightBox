// import { h } from 'preact'; // Removed unused import h
import type { ComponentChildren } from 'preact'; // Corrected import for type
import './Modal.css';

// This Modal is the generic one driven by uiStore for simple alerts/confirmations.
// The WorkspaceModal will be a more specific modal.
// We might need to differentiate or enhance this Modal if it's to be used by WorkspaceModal directly
// or if WorkspaceModal should implement its own modal structure.

// For now, let's assume WorkspaceModal will use this Modal component.
// This means this Modal needs to accept children and other props.

interface ModalProps {
  show: boolean;
  onClose: () => void;
  title?: string;
  children: ComponentChildren; // To allow content to be passed into the modal
  footer?: ComponentChildren; // Optional footer content
}

export const Modal = ({ show, onClose, title, children, footer }: ModalProps) => {
  if (!show) {
    return null;
  }

  return (
    <div class="modal-overlay" onClick={onClose}>
      <div class="modal-content" onClick={(e) => e.stopPropagation()}>
        {title && <div class="modal-header">
          <h2>{title}</h2>
          <button onClick={onClose} class="modal-close-button">&times;</button>
        </div>}
        <div class="modal-body">
          {children}
        </div>
        {footer && (
          <div class="modal-footer">
            {footer}
          </div>
        )}
      </div>
    </div>
  );
};