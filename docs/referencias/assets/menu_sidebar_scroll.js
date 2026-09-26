const scrollContent = document.querySelector('#fdz-menu-sidebar-scroll .sidebar_scroll_content');
if (scrollContent) {
    scrollContent.addEventListener('scroll', hideAllDropdowns);

    let dropdowns = document.querySelectorAll("#sidebar_scroll_nav  li");
    dropdowns.forEach(function (dropdown) {
        setAcessibilitySidebar(dropdown);
        dropdown.addEventListener("mouseenter", toggleDropdown.bind(null, dropdown, true));
        dropdown.addEventListener("mouseleave", toggleDropdownLeave.bind(null, dropdown, false));
        dropdown.addEventListener("touchstart", toggleDropdown.bind(null, dropdown, true));
        dropdown.addEventListener("touchend", toggleDropdownLeave.bind(null, dropdown, false));
        dropdown.addEventListener("click", () => {
            let dropdownList = dropdown.querySelector('.dropdown-list');
            if (!dropdownList) return;
            if (dropdownList.style.display == 'block') {
                this.toggleDropdownLeave(dropdown, false);
            }
            else {
                this.toggleDropdown(dropdown, true);
            }
        });
    });

    function setAcessibilitySidebar(dropdown) {
        parentCloseChildrenOnNavigate(dropdown);
        let childrenDropdown = dropdown.querySelectorAll("ul .dropdown-list");
        childrenDropdown.forEach(function (childrenDropdown) {
            parentGoInsideChildrenWithArrow(dropdown, childrenDropdown);
            childrenDropdown.addEventListener("keydown", (event) => {
                this.closeDropdownChildrenOnEsc(dropdown, event);
                this.childrenCloseOnNavigate(dropdown, childrenDropdown, event);
                this.childrenNavigateWithArrow(childrenDropdown, event);
            });
        });
    }

    function parentCloseChildrenOnNavigate(dropdown) {
        dropdown.addEventListener("keydown", (event) => {
            this.closeDropdownChildrenOnEsc(dropdown, event);
            let isTabPressed = event.key === 'Tab' || event.keyCode === 9;
            if (isTabPressed && event.shiftKey) {
                const actualFocusableElement = document.activeElement.parentElement;
                if (actualFocusableElement === dropdown) this.toggleDropdownLeave(dropdown, false);
            }
        });
    }

    function parentGoInsideChildrenWithArrow(dropdown, childrenDropdown) {
        dropdown.addEventListener("keyup", (event) => {
            if (dropdown.querySelector('a').ariaExpanded == 'true' && document.activeElement.parentElement === dropdown) {
                const focusableContent = childrenDropdown.querySelectorAll('a');
                if (event.key == 'ArrowDown' || event.keyCode == 40) {
                    focusableContent[0].focus();
                }
                if (event.key == 'ArrowUp' || event.keyCode == 38) {
                    focusableContent[focusableContent.length - 1].focus();
                }
            }
        });
    }

    function closeDropdownChildrenOnEsc(dropdown, event) {
        let isEscPressed = event.key === 'Escape' || event.keyCode === 27;
        if(isEscPressed) {
            dropdown.querySelector('a').focus();
            this.toggleDropdownLeave(dropdown, false);
        }
    }

    function childrenCloseOnNavigate(dropdown, childrenDropdown, event) {
        let isTabPressed = event.key === 'Tab' || event.keyCode === 9;
        if (!isTabPressed) return;

        const actualFocusableElement = document.activeElement;
        if (event.shiftKey) {
            const firstFocusableElement = childrenDropdown.querySelectorAll('[tabindex]:not([tabindex="-1"]')[0] ?? document.createElement('div');
            if (actualFocusableElement === firstFocusableElement) this.toggleDropdownLeave(dropdown, false);
        } else {
            const focusableContent = childrenDropdown.querySelectorAll('[tabindex]:not([tabindex="-1"]');
            const lastFocusableElement = focusableContent[focusableContent.length - 1] ?? document.createElement('div');
            if (actualFocusableElement === lastFocusableElement) this.toggleDropdownLeave(dropdown, false);
        }
    }

    function childrenNavigateWithArrow(childrenDropdown, event) {
        const focusableContent = childrenDropdown.querySelectorAll('a');
        let key = 0;
        focusableContent.forEach((object, index) => {
          if (object === document.activeElement) {
            key = index;
            return;
          }
        });
        if (event.key == 'ArrowDown' || event.keyCode == 40) {
          if (key == focusableContent.length -1) return;
          focusableContent[key + 1].focus();
        }
        if (event.key == 'ArrowUp' || event.keyCode == 38) {
          if (key == 0) return;
          focusableContent[key - 1].focus();
        }
    }

    function toggleDropdown(dropdown, show) {
        let top = scrollContent.scrollTop;

        let dropdownList = dropdown.querySelector('.dropdown-list');
        let dropdownObjetives = dropdown.querySelector('.submenu-objetives');
        let dropdownPersonalDepartment = dropdown.querySelector('.submenu-personal_department');


        if(dropdownList) {
            const windowHeight = window.innerHeight;
            let firstLiChild = dropdownList.querySelector('li:first-child');

            dropdownList.style.display = show ? 'block' : 'none';
            dropdownList.style.visibility = show ? 'visible' : 'hidden';
            dropdown.querySelector('a').ariaExpanded = show;
            let dropdownListBottom = dropdownList.getBoundingClientRect().bottom;

            if (dropdownListBottom < windowHeight) {
                dropdownList.style.transform = `translateY(calc(-50% - ${top}px))`;
            }
            if (dropdownListBottom < windowHeight && dropdownObjetives) {
                dropdownList.style.transform = `translateY(calc(-50px - ${top}px))`;
            }
            if (dropdownListBottom > windowHeight) {
                dropdownList.style.transform = `translateY(calc(-100% - ${top}px))`;
            }
            if (dropdownListBottom > windowHeight && dropdownPersonalDepartment) {
                dropdownList.style.transform = `translateY(calc(-85% - ${top}px))`;
            }
            if (!isElementInViewport(firstLiChild)) {
                dropdownList.style.transform = `translateY(calc(-${dropdownList.offsetHeight}px + 12%))`;
            }
            if (dropdownObjetives && !isElementInViewport(firstLiChild)) {
                dropdownObjetives.style.transform = `translateY(calc(-50px - ${top}px))`;
                dropdownObjetives.style.maxHeight = `420px !important`;
                dropdownObjetives.style.overflowY = 'auto';
            }
        }
    }

    function isElementInViewport(el) {
        let rect = el.getBoundingClientRect();
        return (
            rect.top >= 0 &&
            rect.left >= 0 &&
            rect.bottom <= (window.innerHeight || document.documentElement.clientHeight) &&
            rect.right <= (window.innerHeight || document.documentElement.clientHeight)
        );
    }

    function toggleDropdownLeave(dropdown, show) {
        let dropdownList = dropdown.querySelector('.dropdown-list');

        if(dropdownList) {
            dropdownList.style.visibility = show ? 'visible' : 'hidden';
            dropdownList.style.display = 'none';
            dropdownList.style.transform = `none`;
            dropdown.querySelector('a').ariaExpanded = show;
        }
    }

    function hideAllDropdowns() {
        let dropdownLists = document.querySelectorAll('.dropdown-list');
        if(dropdownLists) {
            dropdownLists.forEach(function (dropdownList) {
                dropdownList.style.visibility = 'hidden';
                dropdownList.querySelector('a').ariaExpanded = false;
            });
        }
    }
}



