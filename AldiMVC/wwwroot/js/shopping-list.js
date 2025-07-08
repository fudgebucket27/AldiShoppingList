(function () {
    const ENDPOINT = '/api/Products/search/'
    const DEBOUNCE_MS = 250;

    const list = new Map();
    const listOrder = [];
    let typingTimer;

    // DOM caches for both desktop and mobile
    const $searchInput = $('#searchInput');
    const $searchInputMobile = $('#searchInputMobile');
    const $results = $('#results');
    const $resultsMobile = $('#resultsMobile');
    const $listBody = $('#listTable tbody');
    const $listBodyMobile = $('#listTableMobile tbody');
    const $totalRow = $('#totalRow');
    const $totalRowMobile = $('#totalRowMobile');
    const $grandTotal = $('#grandTotal');
    const $grandTotalMobile = $('#grandTotalMobile');
    const $printBtn = $('#printBtn');
    const $printBtnMobile = $('#printBtnMobile');
    const $loader = $('#loader');
    const $loaderMobile = $('#loaderMobile');
    const $emptyMessage = $('#emptyMessage');
    const $emptyMessageMobile = $('#emptyMessageMobile');
    const $copyBtn = $('#copyChecklistBtn');
    const $copyBtnMobile = $('#copyChecklistBtnMobile');
    const $cartBadge = $('#cartBadge');

    const dollars = c => (c / 100).toFixed(2);

    // Prevent form submit
    $('#searchForm, #searchFormMobile').on('submit', e => e.preventDefault());

    // Debounced search for both inputs
    function setupSearch($input, $resultsContainer, $loaderElement) {
        $input.on('input', function () {
            clearTimeout(typingTimer);
            const q = this.value.trim();

            // Sync both search inputs
            if ($input.is($searchInput)) {
                $searchInputMobile.val(q);
            } else {
                $searchInput.val(q);
            }

            if (!q) {
                const emptyHTML = `
                            <div class="text-center mt-5">
                              <i class="bi bi-search text-muted" style="font-size: 4rem;"></i>
                              <p class="text-muted mt-3">Start typing above to search for Thuringowa Aldi products</p>
                            </div>
                          `;
                $results.html(emptyHTML);
                $resultsMobile.html(emptyHTML);
                return;
            }
            else if (q.length < 3) {
                const minLengthHTML = `
                            <div class="text-center mt-5">
                              <i class="bi bi-info-circle text-muted" style="font-size: 3rem;"></i>
                              <p class="text-muted mt-3">Type at least 3 letters to search.</p>
                            </div>
                          `;
                $results.html(minLengthHTML);
                $resultsMobile.html(minLengthHTML);
                return;
            }

            typingTimer = setTimeout(() => doSearch(q), DEBOUNCE_MS);
        });
    }

    setupSearch($searchInput, $results, $loader);
    setupSearch($searchInputMobile, $resultsMobile, $loaderMobile);

    function doSearch(query) {
        toggleLoading(true);
        $.getJSON(ENDPOINT + encodeURIComponent(query))
            .done(res => renderResults(res))
            .fail(() => {
                const errorHTML = '<div class="alert alert-danger">Error fetching results. Please try again.</div>';
                $results.html(errorHTML);
                $resultsMobile.html(errorHTML);
            })
            .always(() => toggleLoading(false));
    }

    function toggleLoading(state) {
        $loader.toggleClass('d-none', !state);
        $loaderMobile.toggleClass('d-none', !state);
    }

    function renderResults(products) {
        const resultsHtml = products.length ? products.map(p => {
            if (!p.ImageUrl) {
                p.ImageUrl = '/images/404.svg';
            }

            const img = p.ImageUrl.replace('{width}', '256')?.replace('{slug}', p.UrlSlugText) || '';
            return `
                        <div class="card mb-3 product-card shadow-sm">
                          <div class="row g-0">
                            <div class="col-auto">
                              <img src="${img}" class="img-fluid rounded-start" alt="${p.Name}">
                            </div>
                            <div class="col">
                              <div class="card-body d-flex flex-column h-100">
                                <h6 class="card-title mb-1">${p.Name}</h6>
                                <small class="text-muted mb-2">${p.BrandName || ''}</small>
                                <div class="mt-auto">
                                  <div class="d-flex justify-content-between align-items-center">
                                    <span class="fw-semibold fs-5">${p.PriceAmountRelevantDisplay}</span>
                                    <div class="input-group input-group-sm" style="width: 125px;">
                                      <input type="number" class="form-control qty-input" value="1" min="1">
                                      <button class="btn btn-primary add-btn" data-sku="${p.Sku}" title="Add to list">
                                        <i class="bi bi-plus-circle"></i>
                                      </button>
                                    </div>
                                  </div>
                                </div>
                              </div>
                            </div>
                          </div>
                        </div>
                      `;
        }).join('') : `
                    <div class="text-center mt-5">
                      <i class="bi bi-exclamation-circle text-muted" style="font-size: 4rem;"></i>
                      <p class="text-muted mt-3">No products found for your search.</p>
                    </div>
                  `;

        $results.html(resultsHtml);
        $resultsMobile.html(resultsHtml);

        // Attach event handlers
        $('.add-btn').on('click', function () {
            const sku = $(this).data('sku');
            const product = products.find(p => p.Sku === sku);
            const qty = parseInt($(this).siblings('.qty-input').val()) || 1;
            addItem(product, qty);
        });
    }

    function addItem(prod, qty) {
        const ex = list.get(prod.Sku);
        if (ex) {
            ex.qty += qty;
        } else {
            list.set(prod.Sku, { name: prod.Name, priceCents: prod.PriceAmountRelevant, qty });
            listOrder.push(prod.Sku);
        }
        renderList();
        saveList(); 
    }

    function renderList() {
        renderListTable($listBody, false);
        renderListTable($listBodyMobile, true);
        updateCartBadge();
    }

    function renderListTable($tbody, isMobile) {
        $tbody.empty();
        let total = 0;

        listOrder.forEach((sku, index) => {
            const item = list.get(sku);
            if (!item) return;

            const sub = item.qty * item.priceCents;
            total += sub;

            const row = $(
                `<tr class="draggable-row" draggable="true" data-sku="${sku}" data-index="${index}">
                          <td class="no-print text-center">
                            <i class="bi bi-grip-vertical drag-handle"></i>
                          </td>
                          <td>${item.name}</td>
                          <td class="text-end">
                            <div class="input-group input-group-sm justify-content-end no-print" style="max-width:${isMobile ? '120px' : '140px'};">
                              <button class="btn btn-outline-secondary minus-btn" data-sku="${sku}">−</button>
                              <input type="number" class="form-control qty-input qty-edit" data-sku="${sku}" min="1" value="${item.qty}">
                              <button class="btn btn-outline-secondary plus-btn" data-sku="${sku}">+</button>
                            </div>
                            <span class="qty-print">${item.qty}</span>
                          </td>
                          <td class="text-end">$${dollars(item.priceCents)}</td>
                          <td class="text-end">$${dollars(sub)}</td>
                          <td class="no-print text-end">
                            <button class="btn btn-outline-danger btn-sm remove-btn" data-sku="${sku}" title="Remove item">
                              <i class="bi bi-trash"></i>
                            </button>
                          </td>
                        </tr>`
            );

            // Attach event handlers
            row.find('.minus-btn').on('click', () => updateQty(sku, item.qty - 1));
            row.find('.plus-btn').on('click', () => updateQty(sku, item.qty + 1));
            row.find('.qty-edit').on('change', function () { updateQty(sku, parseInt(this.value) || 1); });
            row.find('.remove-btn').on('click', () => removeItem(sku));

            // Add drag and drop handlers
            row.on('dragstart', handleDragStart);
            row.on('dragover', handleDragOver);
            row.on('drop', handleDrop);
            row.on('dragend', handleDragEnd);
            row.on('dragenter', handleDragEnter);
            row.on('dragleave', handleDragLeave);

            $tbody.append(row);
        });

        // Update totals and visibility
        const has = list.size > 0;
        $grandTotal.text(`$${dollars(total)}`);
        $grandTotalMobile.text(`$${dollars(total)}`);
        $totalRow.toggleClass('d-none', !has);
        $totalRowMobile.toggleClass('d-none', !has);
        $printBtn.toggleClass('d-none', !has);
        $printBtnMobile.toggleClass('d-none', !has);
        $copyBtn.toggleClass('d-none', !has);
        $copyBtnMobile.toggleClass('d-none', !has);
        $emptyMessage.toggleClass('d-none', has);
        $emptyMessageMobile.toggleClass('d-none', has);
    }

    function updateCartBadge() {
        const totalItems = Array.from(list.values()).reduce((sum, item) => sum + item.qty, 0);
        if (totalItems > 0) {
            $cartBadge.text(totalItems).removeClass('d-none');
        } else {
            $cartBadge.addClass('d-none');
        }
    }

    function updateQty(sku, n) {
        if (n < 1) {
            list.delete(sku);
            const index = listOrder.indexOf(sku);
            if (index > -1) listOrder.splice(index, 1);
        } else {
            list.get(sku).qty = n;
        }
        renderList();
        saveList(); 
    }

    function removeItem(sku) {
        list.delete(sku);
        const index = listOrder.indexOf(sku);
        if (index > -1) listOrder.splice(index, 1);
        renderList();
        saveList(); 
    }

    // Print functionality
    $printBtn.add($printBtnMobile).on('click', () => window.print());

    // Copy functionality
    function copyChecklist() {
        let checklistText = '';
        listOrder.forEach(sku => {
            const item = list.get(sku);
            if (!item) return;
            checklistText += `${item.qty} × ${item.name}\n`;
        });

        if (!navigator.clipboard) {
            alert("Clipboard not supported. Please copy manually.");
            return;
        }

        navigator.clipboard.writeText(checklistText)
            .then(() => alert("Checklist copied! Paste into Notes."))
            .catch(() => alert("Failed to copy to clipboard."));
    }

    $copyBtn.add($copyBtnMobile).on('click', copyChecklist);

    // Drag and drop variables
    let draggedElement = null;
    let draggedSku = null;

    // Drag and drop handlers
    function handleDragStart(e) {
        draggedElement = this;
        draggedSku = $(this).data('sku');
        $(this).addClass('dragging');
        e.originalEvent.dataTransfer.effectAllowed = 'move';
        e.originalEvent.dataTransfer.setData('text/html', this.outerHTML);
    }

    function handleDragOver(e) {
        if (e.preventDefault) e.preventDefault();
        e.originalEvent.dataTransfer.dropEffect = 'move';
        return false;
    }

    function handleDragEnter(e) {
        if (this !== draggedElement) {
            $(this).addClass('drag-over');
        }
    }

    function handleDragLeave(e) {
        $(this).removeClass('drag-over');
    }

    function handleDrop(e) {
        if (e.stopPropagation) e.stopPropagation();

        if (this !== draggedElement) {
            const draggedIndex = listOrder.indexOf(draggedSku);
            const targetSku = $(this).data('sku');
            const targetIndex = listOrder.indexOf(targetSku);

            // Remove dragged item from its current position
            listOrder.splice(draggedIndex, 1);

            // Insert at new position
            listOrder.splice(targetIndex, 0, draggedSku);

            renderList();
            saveList();
        }

        return false;
    }

    function handleDragEnd(e) {
        $('.draggable-row').removeClass('dragging drag-over');
        draggedElement = null;
        draggedSku = null;
    }

    function saveList() {
        const data = {
            list: Array.from(list.entries()),
            listOrder
        };
        localStorage.setItem('shoppingList', JSON.stringify(data));
    }

    function loadList() {
        const data = localStorage.getItem('shoppingList');
        if (!data) return;

        try {
            const { list: entries, listOrder: order } = JSON.parse(data);
            list.clear();
            entries.forEach(([sku, item]) => list.set(sku, item));
            listOrder.length = 0;
            listOrder.push(...order);
        } catch (e) {
            console.error('Failed to load list from localStorage:', e);
            localStorage.removeItem('shoppingList');
        }
    }

    loadList();
    renderList();
})();