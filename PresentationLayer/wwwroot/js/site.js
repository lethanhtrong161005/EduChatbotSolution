const HubMethod = Object.freeze({
    JoinResourceType: "SubscribeToResourceType",
    JoinResource: "SubscribeToResource",
    JoinResourceTypeCollection: "SubscribeToResourceTypeCollection",
    JoinResourceCollection: "SubscribeToResourceCollection",

    LeaveResourceType: "UnsubscribeFromResourceType",
    LeaveResource: "UnsubscribeFromResource",
    LeaveResourceTypeCollection: "UnsubscribeFromResourceTypeCollection",
    LeaveResourceCollection: "UnsubscribeFromResourceCollection",
});

const ResourceType = Object.freeze({
    User: "user",
    Membership: "membership",
    Subject: "subject",
    Chapter: "chapter",
    Document: "document",
    DocumentChapter: "document-chapter",
    ChatSession: "chat-session",
});

const ResourceAction = Object.freeze({
    Created: "created",
    Updated: "updated",
    Deleted: "deleted",
    Disabled: "disabled",
    Enabled: "enabled",
});

function getKey(obj, val) {

    return Object.keys(obj).find(x => obj[x] === val);
}

function getValue(obj, val) {

    return Object.values(obj).find(x => x === val);
}

function getAntiForgery() {

    return document.querySelector("input[name='__RequestVerificationToken']")?.value ?? "";
}

// ── Global Toast System (UI/UX compliant) ───────────────────
(function () {
    const toastStyles = `
        .toast-container-global {
            position: fixed;
            top: 24px;
            left: 50%;
            transform: translateX(-50%);
            z-index: 100000;
            display: flex;
            flex-direction: column;
            align-items: center;
            gap: 12px;
            pointer-events: none;
            font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
        }
        .toast-item-global {
            pointer-events: auto;
            display: flex;
            align-items: center;
            gap: 12px;
            padding: 14px 22px;
            border-radius: 24px;
            font-size: 14px;
            font-weight: 600;
            box-shadow: 0 10px 30px -5px rgba(0, 0, 0, 0.08), 0 4px 12px -2px rgba(0, 0, 0, 0.03);
            min-width: 320px;
            max-width: 450px;
            transition: all 0.3s cubic-bezier(0.16, 1, 0.3, 1);
            animation: toastInSpringGlobal 0.45s cubic-bezier(0.34, 1.56, 0.64, 1);
        }
        .toast-item-global:hover {
            transform: translateY(-2px);
            box-shadow: 0 14px 35px -5px rgba(0, 0, 0, 0.12), 0 6px 16px -2px rgba(0, 0, 0, 0.04);
        }
        .toast-item-global i {
            font-size: 18px;
            flex-shrink: 0;
        }
        .toast-item-global span {
            flex-grow: 1;
            line-height: 1.45;
        }
        .toast-global-close {
            background: none;
            border: none;
            font-size: 18px;
            line-height: 1;
            cursor: pointer;
            color: inherit;
            opacity: 0.6;
            padding: 0 0 0 8px;
            transition: opacity 0.2s ease, transform 0.2s ease;
            display: inline-flex;
            align-items: center;
            justify-content: center;
            flex-shrink: 0;
        }
        .toast-global-close:hover {
            opacity: 1;
            transform: scale(1.15);
        }
        .toast-item-global.success {
            background-color: #f0fdfa;
            border: 2px solid #0d9488;
            color: #0d9488;
        }
        .toast-item-global.error {
            background-color: #fff1f2;
            border: 2px solid #f43f5e;
            color: #e11d48;
        }
        .toast-item-global.warning {
            background-color: #fff7ed;
            border: 2px solid #f97316;
            color: #ea580c;
        }
        .toast-item-global.info {
            background-color: #f0f9ff;
            border: 2px solid #0ea5e9;
            color: #0284c7;
        }
        @keyframes toastInSpringGlobal {
            0% {
                opacity: 0;
                transform: translateY(-20px) scale(0.92);
            }
            100% {
                opacity: 1;
                transform: translateY(0) scale(1);
            }
        }
        @media (max-width: 576px) {
            .toast-container-global {
                top: 16px;
                right: 16px;
                left: 16px;
                width: calc(100% - 32px);
                transform: none;
                left: 16px;
            }
            .toast-item-global {
                width: 100%;
                min-width: 0;
                max-width: none;
            }
        }
    `;

    // Inject styles dynamically
    function injectStyles() {
        if (document.getElementById('global-toast-styles')) return;
        const style = document.createElement('style');
        style.id = 'global-toast-styles';
        style.textContent = toastStyles;
        document.head.appendChild(style);
    }

    // Main show function
    window.showToast = function (param1, param2) {
        injectStyles();

        // Ensure container exists
        let container = document.querySelector('.toast-container-global');
        if (!container) {
            container = document.createElement('div');
            container.className = 'toast-container-global';
            document.body.appendChild(container);
        }

        // Handle signature mismatch (param1 as message/type or param2 as message/type)
        let message = '';
        let type = 'success';
        const types = ['success', 'error', 'warning', 'info'];

        if (types.includes(param1)) {
            type = param1;
            message = param2;
        } else if (types.includes(param2)) {
            type = param2;
            message = param1;
        } else {
            message = param1 || '';
            type = param2 || 'success';
        }

        const icons = {
            success: 'fa-solid fa-circle-check',
            error: 'fa-solid fa-circle-xmark',
            warning: 'fa-solid fa-triangle-exclamation',
            info: 'fa-solid fa-circle-info'
        };
        const iconClass = icons[type] || 'fa-solid fa-circle-info';

        const toast = document.createElement('div');
        toast.className = `toast-item-global ${type}`;
        toast.innerHTML = `
            <i class="${iconClass}"></i>
            <span>${message}</span>
            <button type="button" class="toast-global-close">&times;</button>
        `;

        container.appendChild(toast);

        // Bind close event
        const closeBtn = toast.querySelector('.toast-global-close');
        closeBtn?.addEventListener('click', () => dismiss(toast));

        // Auto dismiss
        const timeoutId = setTimeout(() => dismiss(toast), 4500);

        function dismiss(el) {
            clearTimeout(timeoutId);
            el.style.animation = 'none';
            el.style.opacity = '0';
            el.style.transform = 'translateY(-10px) scale(0.95)';
            el.style.transition = 'all 0.3s ease';
            setTimeout(() => el.remove(), 300);
        }
    };

    // Auto-convert standard static alerts to beautiful global toasts
    document.addEventListener('DOMContentLoaded', () => {
        document.querySelectorAll('.alert-success').forEach(el => {
            const tempEl = el.cloneNode(true);
            tempEl.querySelectorAll('i, button').forEach(child => child.remove());
            const msg = tempEl.textContent.trim();
            if (msg) {
                window.showToast(msg, 'success');
            }
            el.style.display = 'none';
            el.remove();
        });

        document.querySelectorAll('.alert-error').forEach(el => {
            const listItems = el.querySelectorAll('li');
            if (listItems.length > 0) {
                listItems.forEach(li => {
                    const text = li.textContent.trim();
                    if (text && text !== 'Please correct the errors and try again.') {
                        window.showToast(text, 'error');
                    }
                });
            } else {
                const tempEl = el.cloneNode(true);
                tempEl.querySelectorAll('i, button').forEach(child => child.remove());
                const msg = tempEl.textContent.trim();
                if (msg) {
                    window.showToast(msg, 'error');
                }
            }
            el.style.display = 'none';
            el.remove();
        });
    });
})();
