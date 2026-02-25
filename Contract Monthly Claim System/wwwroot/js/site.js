document.addEventListener('DOMContentLoaded', function () {
    initializeSidebar();
    initializeAnimations();

    if (document.querySelector('.dashboard-stats')) {
        initializeDashboard();
    }
});

function initializeSidebar() {
    const sidebar = document.getElementById('sidebar');
    const mainContent = document.querySelector('.main-content');
    const toggleButtons = document.querySelectorAll('.sidebar-toggle');

    // Check saved state
    const isCollapsed = localStorage.getItem('sidebarCollapsed') === 'true';
    if (isCollapsed) {
        sidebar.classList.add('collapsed');
        mainContent.classList.add('expanded');
    }

    // Toggle button listeners
    toggleButtons.forEach(btn => {
        btn.addEventListener('click', function () {
            sidebar.classList.toggle('collapsed');
            mainContent.classList.toggle('expanded');

            // Save state
            const isNowCollapsed = sidebar.classList.contains('collapsed');
            localStorage.setItem('sidebarCollapsed', isNowCollapsed);
        });
    });
}

function initializeAnimations() {
    // Simple hover effects
    const cards = document.querySelectorAll('.card-hover');
    cards.forEach(card => {
        card.addEventListener('mouseenter', function () {
            this.style.transform = 'translateY(-3px)';
        });

        card.addEventListener('mouseleave', function () {
            this.style.transform = '';
        });
    });
}

// Dashboard specific functionality
function initializeDashboard() {
    // Initialize tooltips
    const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    const tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });

    // Animate progress bars when they come into view
    const progressBars = document.querySelectorAll('.progress-bar');
    progressBars.forEach(bar => {
        const width = bar.style.width;
        bar.style.width = '0%';
        setTimeout(() => {
            bar.style.transition = 'width 1s ease-in-out';
            bar.style.width = width;
        }, 100);
    });

    // Add click handlers for quick actions
    const quickActionCards = document.querySelectorAll('.quick-action-card');
    quickActionCards.forEach(card => {
        card.addEventListener('click', function () {
            const action = this.getAttribute('data-action');
            if (action) {
                window.location.href = action;
            }
        });
    });
}