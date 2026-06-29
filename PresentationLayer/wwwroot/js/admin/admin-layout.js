/**
 * admin-layout.js
 * EduChatAI Admin Panel — sidebar toggle & responsive behaviour
 */

(function () {
    'use strict';

    const sidebar = document.getElementById('adminSidebar');
    const main = document.getElementById('adminMain');
    const toggle = document.getElementById('sidebarToggle');
    const mobileBtn = document.getElementById('mobileMenuBtn');
    const overlay = document.getElementById('sidebarOverlay');

    const COLLAPSED_KEY = 'admin_sidebar_collapsed';

    // ── Desktop: restore collapsed state ──────────────────────────
    if (localStorage.getItem(COLLAPSED_KEY) === '1') {
        sidebar?.classList.add('collapsed');
        main?.classList.add('collapsed');
    }

    // ── Desktop toggle ────────────────────────────────────────────
    toggle?.addEventListener('click', () => {
        sidebar.classList.toggle('collapsed');
        main.classList.toggle('collapsed');
        localStorage.setItem(COLLAPSED_KEY,
            sidebar.classList.contains('collapsed') ? '1' : '0');
    });

    // ── Mobile menu open ──────────────────────────────────────────
    mobileBtn?.addEventListener('click', openMobile);
    overlay?.addEventListener('click', closeMobile);

    function openMobile() {
        sidebar?.classList.add('mobile-open');
        overlay?.classList.add('active');
        document.body.style.overflow = 'hidden';
    }

    function closeMobile() {
        sidebar?.classList.remove('mobile-open');
        overlay?.classList.remove('active');
        document.body.style.overflow = '';
    }

    // ── Auto-dismiss toasts ────────────────────────────────────────
    document.querySelectorAll('.admin-toast').forEach(toast => {
        setTimeout(() => toast.style.opacity = '0', 4000);
        setTimeout(() => toast.remove(), 4300);
    });
}());
