document.addEventListener("DOMContentLoaded", () => {
  initializeCommonUi();
  initializeInventoryPage();
});

function initializeCommonUi() {
  document.querySelectorAll(".clickable-row").forEach((row) => {
    if (row.dataset.clickableBound === "true") {
      return;
    }

    row.dataset.clickableBound = "true";
    row.addEventListener("click", (event) => {
      if (event.target.closest("input, button, a, label, select, textarea, form")) {
        return;
      }

      const href = row.dataset.href;
      if (href) {
        window.location.href = href;
      }
    });
  });

  document.querySelectorAll("[data-select-all]").forEach((toggle) => {
    if (toggle.dataset.selectAllBound === "true") {
      return;
    }

    toggle.dataset.selectAllBound = "true";
    toggle.addEventListener("change", () => {
      const table = toggle.closest("table");
      table?.querySelectorAll('tbody input[type="checkbox"]').forEach((checkbox) => {
        checkbox.checked = toggle.checked;
        checkbox.dispatchEvent(new Event("change", { bubbles: true }));
      });
    });
  });

  document.querySelectorAll('tbody input[type="checkbox"]').forEach((checkbox) => {
    if (checkbox.dataset.syncBound === "true") {
      return;
    }

    checkbox.dataset.syncBound = "true";
    checkbox.addEventListener("change", () => {
      const row = checkbox.closest("tr");
      row?.querySelectorAll(`input[type="checkbox"][value="${CSS.escape(checkbox.value)}"]`).forEach((sibling) => {
        sibling.checked = checkbox.checked;
      });
    });
  });

  document.querySelectorAll('[data-bs-toggle="popover"]').forEach((element) => {
    if (element.dataset.popoverBound === "true") {
      return;
    }

    element.dataset.popoverBound = "true";
    new bootstrap.Popover(element);
  });

  initializeTableFilters();
  initializeSortableTables();
}

function initializeInventoryPage() {
  initializeDragLists();
  initializeBuilderButtons();
  initializeUserLookup();
  initializeTagLookup();
  initializeAutosaveForms();
  initializeDiscussionFeed();
  initializeAccessSorting();
  initializeSelectedUserRemoval();
  initializeCloudImageUpload();
  initializeSampleFillers();
  initializeCommonUi();
  updateCustomIdPreview();
}

function initializeDragLists() {
  document.querySelectorAll("#custom-id-list, #field-definition-list").forEach((list) => {
    list.querySelectorAll(".builder-row").forEach((row) => bindDraggableRow(list, row));
  });
}

function bindDraggableRow(list, row) {
  if (row.dataset.dragBound === "true") {
    return;
  }

  row.dataset.dragBound = "true";
  let dragged = null;

  row.addEventListener("dragstart", () => {
    dragged = row;
    row.classList.add("opacity-50");
  });

  row.addEventListener("dragend", () => {
    row.classList.remove("opacity-50");
    syncIndexedNames(list);
    updateCustomIdPreview();
  });

  row.addEventListener("dragover", (event) => {
    event.preventDefault();
    const target = event.currentTarget;
    if (dragged && target !== dragged) {
      const rect = target.getBoundingClientRect();
      const before = event.clientY < rect.top + rect.height / 2;
      list.insertBefore(dragged, before ? target : target.nextSibling);
    }
  });
}

function initializeBuilderButtons() {
  const customIdList = document.getElementById("custom-id-list");
  const fieldList = document.getElementById("field-definition-list");

  if (customIdList && customIdList.dataset.previewBound !== "true") {
    customIdList.dataset.previewBound = "true";
    customIdList.addEventListener("input", updateCustomIdPreview);
    customIdList.addEventListener("change", updateCustomIdPreview);
  }

  const addCustomIdButton = document.getElementById("add-custom-id-row");
  if (addCustomIdButton && addCustomIdButton.dataset.builderBound !== "true") {
    addCustomIdButton.dataset.builderBound = "true";
    addCustomIdButton.addEventListener("click", () => {
      if (!customIdList) {
        return;
      }

      const index = customIdList.querySelectorAll(".builder-row").length;
      customIdList.insertAdjacentHTML("beforeend", `
      <div class="builder-row" draggable="true">
        <input type="hidden" name="elements[${index}].SortOrder" value="${index}" class="sort-order-input" />
        <input type="hidden" name="elements[${index}].Id" value="0" />
        <span class="builder-grip" title="Drag to reorder">Drag</span>
        <select name="elements[${index}].ElementType" class="form-select">
          <option value="Fixed">Fixed</option>
          <option value="Random20Bit">Random20Bit</option>
          <option value="Random32Bit">Random32Bit</option>
          <option value="Random6Digit">Random6Digit</option>
          <option value="Random9Digit">Random9Digit</option>
          <option value="Guid">Guid</option>
          <option value="DateTime">DateTime</option>
          <option value="Sequence">Sequence</option>
        </select>
        <input name="elements[${index}].FixedText" class="form-control" placeholder="Fixed text" />
        <input name="elements[${index}].Format" class="form-control" placeholder="Format" />
        <button type="button" class="btn btn-outline-secondary builder-help" data-bs-toggle="popover" data-bs-content="Examples: D4, yyyy, X5.">?</button>
        <button type="button" class="btn btn-outline-danger builder-remove">Remove</button>
      </div>`);

      initializeInventoryPage();
    });
  }

  const addFieldButton = document.getElementById("add-field-row");
  if (addFieldButton && addFieldButton.dataset.builderBound !== "true") {
    addFieldButton.dataset.builderBound = "true";
    addFieldButton.addEventListener("click", () => {
      if (!fieldList) {
        return;
      }

      const index = fieldList.querySelectorAll(".builder-row").length;
      fieldList.insertAdjacentHTML("beforeend", `
      <div class="builder-row field-builder-row" draggable="true">
        <input type="hidden" name="fields[${index}].SortOrder" value="${index}" class="sort-order-input" />
        <input type="hidden" name="fields[${index}].Id" value="0" />
        <span class="builder-grip" title="Drag to reorder">Drag</span>
        <select name="fields[${index}].FieldType" class="form-select">
          <option value="SingleLineText">SingleLineText</option>
          <option value="MultiLineText">MultiLineText</option>
          <option value="Number">Number</option>
          <option value="Link">Link</option>
          <option value="Boolean">Boolean</option>
          <option value="Select">Select</option>
        </select>
        <input name="fields[${index}].Title" class="form-control" placeholder="Field title" />
        <input name="fields[${index}].Description" class="form-control" placeholder="Description" />
        <input name="fields[${index}].OptionsJson" class="form-control" placeholder='["Option A","Option B"]' />
        <input name="fields[${index}].RegexPattern" class="form-control" placeholder="Regex pattern" />
        <input name="fields[${index}].MaxLength" class="form-control" type="number" min="1" placeholder="Max length" />
        <input name="fields[${index}].MinValue" class="form-control" type="number" step="0.01" placeholder="Min value" />
        <input name="fields[${index}].MaxValue" class="form-control" type="number" step="0.01" placeholder="Max value" />
        <div class="form-check">
          <input type="checkbox" class="form-check-input" name="fields[${index}].IsRequired" value="true" />
          <label class="form-check-label">Required</label>
        </div>
        <div class="form-check">
          <input type="checkbox" class="form-check-input" name="fields[${index}].ShowInTable" value="true" />
          <label class="form-check-label">Show in table</label>
        </div>
        <button type="button" class="btn btn-outline-danger builder-remove">Remove</button>
      </div>`);

      initializeInventoryPage();
    });
  }

  document.querySelectorAll(".builder-remove").forEach((button) => {
    if (button.dataset.removeBound === "true") {
      return;
    }

    button.dataset.removeBound = "true";
    button.addEventListener("click", () => removeBuilderRow(button.closest(".builder-row")));
  });

  document.querySelectorAll(".builder-trash").forEach((trash) => {
    if (trash.dataset.trashBound === "true") {
      return;
    }

    trash.dataset.trashBound = "true";
    trash.addEventListener("dragover", (event) => {
      event.preventDefault();
      trash.classList.add("builder-trash-active");
    });
    trash.addEventListener("dragleave", () => trash.classList.remove("builder-trash-active"));
    trash.addEventListener("drop", (event) => {
      event.preventDefault();
      trash.classList.remove("builder-trash-active");
      removeBuilderRow(document.querySelector(".builder-row.opacity-50"));
    });
  });
}

function removeBuilderRow(row) {
  if (!row) {
    return;
  }

  const list = row.parentElement;
  row.remove();
  if (list) {
    syncIndexedNames(list);
  }
  updateCustomIdPreview();
}

function syncIndexedNames(list) {
  const isCustomId = list.id === "custom-id-list";
  const prefix = isCustomId ? "elements" : "fields";

  list.querySelectorAll(".builder-row").forEach((row, index) => {
    row.querySelectorAll("input, select, textarea").forEach((input) => {
      if (input.name) {
        input.name = input.name.replace(/^[a-zA-Z]+\[\d+\]/, `${prefix}[${index}]`);
      }
    });

    const sortInput = row.querySelector(".sort-order-input");
    if (sortInput) {
      sortInput.value = index.toString();
    }
  });
  updateCustomIdPreview();
}

function initializeUserLookup() {
  const input = document.getElementById("user-search-box");
  const results = document.getElementById("user-search-results");
  const selected = document.getElementById("selected-users");

  if (!input || !results || !selected || input.dataset.lookupBound === "true") {
    return;
  }

  input.dataset.lookupBound = "true";
  input.addEventListener("input", async () => {
    const query = input.value.trim();
    if (query.length < 2) {
      results.innerHTML = "";
      return;
    }

    const response = await fetch(`${input.dataset.lookupUrl}?q=${encodeURIComponent(query)}`);
    const users = await response.json();
    results.innerHTML = users.map((user) =>
      `<button type="button" class="list-group-item list-group-item-action" data-user-id="${user.id}" data-name="${escapeHtml(user.name ?? "")}" data-email="${escapeHtml(user.email ?? "")}">
        ${escapeHtml(user.name ?? "")} <span class="text-secondary">(${escapeHtml(user.email ?? "")})</span>
      </button>`).join("");

    results.querySelectorAll("button").forEach((button) => {
      button.addEventListener("click", () => {
        const exists = selected.querySelector(`input[value="${button.dataset.userId}"]`);
        if (exists) {
          results.innerHTML = "";
          input.value = "";
          return;
        }

        selected.insertAdjacentHTML("beforeend", `
          <label class="selected-user" data-name="${button.dataset.name.toLowerCase()}" data-email="${button.dataset.email.toLowerCase()}">
            <input type="hidden" name="userIds" value="${button.dataset.userId}" />
            <span>${button.dataset.name} (${button.dataset.email})</span>
            <button type="button" class="btn btn-sm btn-outline-danger selected-user-remove">Remove</button>
          </label>`);
        results.innerHTML = "";
        input.value = "";
        initializeSelectedUserRemoval();
      });
    });
  });
}

function initializeSelectedUserRemoval() {
  document.querySelectorAll(".selected-user-remove").forEach((button) => {
    if (button.dataset.selectedRemoveBound === "true") {
      return;
    }

    button.dataset.selectedRemoveBound = "true";
    button.addEventListener("click", () => button.closest(".selected-user")?.remove());
  });
}

function initializeTagLookup() {
  const input = document.querySelector("[data-tag-lookup-url]");
  const results = document.getElementById("tag-search-results");

  if (!input || !results || input.dataset.tagLookupBound === "true") {
    return;
  }

  input.dataset.tagLookupBound = "true";
  input.addEventListener("input", async () => {
    const lastToken = input.value.split(",").pop()?.trim() ?? "";
    if (!lastToken) {
      results.innerHTML = "";
      return;
    }

    const response = await fetch(`${input.dataset.tagLookupUrl}?q=${encodeURIComponent(lastToken)}`);
    const tags = await response.json();
    results.innerHTML = tags.map((tag) =>
      `<button type="button" class="list-group-item list-group-item-action" data-tag-name="${escapeHtml(tag.name)}">${escapeHtml(tag.name)}</button>`).join("");

    results.querySelectorAll("button").forEach((button) => {
      button.addEventListener("click", () => {
        const values = input.value
          .split(",")
          .map((value) => value.trim())
          .filter((value) => value.length > 0);

        values.pop();
        if (!values.includes(button.dataset.tagName)) {
          values.push(button.dataset.tagName);
        }

        input.value = `${values.join(", ")}${values.length ? ", " : ""}`;
        results.innerHTML = "";
        input.focus();
      });
    });
  });
}

function initializeAutosaveForms() {
  document.querySelectorAll(".autosave-form").forEach((form) => {
    if (form.dataset.autosaveBound === "true") {
      return;
    }

    form.dataset.autosaveBound = "true";
    const status = document.querySelector(form.dataset.autosaveStatus);
    const versionTarget = document.querySelector(form.dataset.versionTarget);
    let dirty = false;
    let saving = false;

    form.addEventListener("input", () => {
      dirty = true;
      if (status) {
        status.textContent = "Changes pending";
      }
    });

    form.addEventListener("change", () => {
      dirty = true;
      if (status) {
        status.textContent = "Changes pending";
      }
    });

    form.addEventListener("submit", async (event) => {
      event.preventDefault();
      await autosaveForm(form, status, versionTarget);
      dirty = false;
    });

    window.setInterval(async () => {
      if (!dirty || saving) {
        return;
      }

      saving = true;
      const success = await autosaveForm(form, status, versionTarget);
      dirty = !success;
      saving = false;
    }, 8000);
  });
}

async function autosaveForm(form, status, versionTarget) {
  if (status) {
    status.textContent = "Saving…";
  }

  const formData = new FormData(form);
  if (!formData.has("IsPublicWrite")) {
    formData.append("IsPublicWrite", "false");
  }

  const response = await fetch(form.action, {
    method: "POST",
    body: formData,
    headers: {
      "X-Requested-With": "XMLHttpRequest"
    }
  });

  if (response.ok) {
    const payload = await response.json();
    const versionInput = form.querySelector('input[name="Version"]');
    if (versionInput) {
      versionInput.value = payload.version;
    }
    if (versionTarget) {
      versionTarget.textContent = `Version ${payload.version}`;
    }
    if (status) {
      status.textContent = `Saved at ${payload.updatedAt}`;
    }
    return true;
  }

  const payload = await response.json().catch(() => ({ message: "Autosave failed." }));
  if (status) {
    status.textContent = payload.message ?? "Autosave failed.";
  }
  return false;
}

function initializeDiscussionFeed() {
  const feed = document.getElementById("discussion-feed");
  if (!feed || feed.dataset.pollBound === "true") {
    return;
  }

  feed.dataset.pollBound = "true";
  const refresh = async () => {
    const response = await fetch(feed.dataset.streamUrl);
    if (!response.ok) {
      return;
    }

    const posts = await response.json();
    feed.innerHTML = posts.map((post) => `
      <article class="discussion-post">
        <div class="d-flex justify-content-between gap-3">
          <a class="fw-semibold text-decoration-none" href="/Profile/Public/${post.authorId}">${escapeHtml(post.authorName)}</a>
          <time class="small text-secondary">${escapeHtml(post.createdAt)}</time>
        </div>
        <div class="mb-0 mt-2 markdown-body">${post.html}</div>
      </article>`).join("");
  };

  window.setInterval(refresh, 4000);
}

function initializeAccessSorting() {
  const selected = document.getElementById("selected-users");
  if (!selected) {
    return;
  }

  document.querySelectorAll(".access-sort-button").forEach((button) => {
    if (button.dataset.sortBound === "true") {
      return;
    }

    button.dataset.sortBound = "true";
    button.addEventListener("click", () => {
      const mode = button.dataset.sortMode;
      const entries = Array.from(selected.querySelectorAll(".selected-user"));
      entries.sort((a, b) => (a.dataset[mode] ?? "").localeCompare(b.dataset[mode] ?? ""));
      entries.forEach((entry) => selected.appendChild(entry));

      document.querySelectorAll(".access-sort-button").forEach((item) => item.classList.remove("active"));
      button.classList.add("active");
    });
  });
}

function initializeTableFilters() {
  document.querySelectorAll(".table-filter").forEach((input) => {
    if (input.dataset.filterBound === "true") {
      return;
    }

    input.dataset.filterBound = "true";
    input.addEventListener("input", () => {
      const table = document.querySelector(input.dataset.filterTable);
      const needle = input.value.trim().toLowerCase();
      table?.querySelectorAll("tbody tr").forEach((row) => {
        row.hidden = needle.length > 0 && !row.textContent.toLowerCase().includes(needle);
      });
    });
  });
}

function initializeCloudImageUpload() {
  document.querySelectorAll(".cloud-image-upload").forEach((input) => {
    const form = input.closest("form");
    if (form && form.dataset.cloudSubmitGuardBound !== "true") {
      form.dataset.cloudSubmitGuardBound = "true";
      form.addEventListener("submit", (event) => {
        const uploading = form.querySelector('.cloud-image-upload[data-uploading="true"]');
        if (!uploading) {
          return;
        }

        event.preventDefault();
        const status = uploading.parentElement?.querySelector(".cloud-image-status");
        if (status) {
          status.textContent = "Please wait until the image upload finishes, then save again.";
        }
      });
    }

    if (input.dataset.cloudUploadBound === "true") {
      return;
    }

    input.dataset.cloudUploadBound = "true";
    input.addEventListener("change", async () => {
      const file = input.files?.[0];
      if (!file) {
        return;
      }

      const status = input.parentElement?.querySelector(".cloud-image-status");
      const urlInput = input.parentElement?.querySelector(".cloud-image-url");
      if (!urlInput) {
        return;
      }

      const formData = new FormData();
      formData.append("file", file);
      formData.append("upload_preset", input.dataset.uploadPreset);
      input.dataset.uploading = "true";

      if (status) {
        status.textContent = "Uploading image...";
      }

      try {
        const response = await fetch(`https://api.cloudinary.com/v1_1/${encodeURIComponent(input.dataset.cloudName)}/image/upload`, {
          method: "POST",
          body: formData
        });

        if (!response.ok) {
          throw new Error("Upload failed");
        }

        const payload = await response.json();
        urlInput.value = payload.secure_url;
        urlInput.dispatchEvent(new Event("input", { bubbles: true }));
        renderCloudImagePreview(input.parentElement, payload.secure_url);
        if (status) {
          status.textContent = "Image uploaded. Save the inventory after the URL appears above.";
        }
      } catch {
        if (status) {
          status.textContent = "Image upload failed. You can still paste a cloud-hosted URL manually.";
        }
      } finally {
        delete input.dataset.uploading;
      }
    });
  });
}

function renderCloudImagePreview(container, url) {
  if (!container || !url) {
    return;
  }

  let preview = container.querySelector(".cloud-image-preview");
  if (!preview) {
    preview = document.createElement("img");
    preview.className = "cloud-image-preview";
    preview.alt = "Uploaded image preview";
    container.appendChild(preview);
  }

  preview.src = url;
}

function initializeSampleFillers() {
  document.querySelectorAll("[data-fill-inventory-sample]").forEach((button) => {
    if (button.dataset.sampleFillBound === "true") {
      return;
    }

    button.dataset.sampleFillBound = "true";
    button.addEventListener("click", () => {
      const form = button.closest("form");
      if (!form) {
        return;
      }

      setFieldValue(form, "[name='Title']", "Company Owned Computers");
      setFieldValue(
        form,
        "[name='Description']",
        "Tracks company laptops, workstations, monitors, and assigned equipment.\n\n**Purpose:** keep asset ownership and prices visible."
      );
      setFieldValue(form, "[name='Category']", "Equipment");
      setFieldValue(form, "[name='Tags']", "equipment, laptop, office");

      const publicWrite = form.querySelector("[name='IsPublicWrite']");
      if (publicWrite) {
        publicWrite.checked = true;
        publicWrite.dispatchEvent(new Event("change", { bubbles: true }));
      }
    });
  });
}

function setFieldValue(root, selector, value) {
  const field = root.querySelector(selector);
  if (!field) {
    return;
  }

  field.value = value;
  field.dispatchEvent(new Event("input", { bubbles: true }));
  field.dispatchEvent(new Event("change", { bubbles: true }));
}

function initializeSortableTables() {
  document.querySelectorAll("th[data-sort-key]").forEach((header) => {
    if (header.dataset.sortBound === "true") {
      return;
    }

    header.dataset.sortBound = "true";
    header.classList.add("sortable-header");
    header.addEventListener("click", () => {
      const table = header.closest("table");
      const body = table?.querySelector("tbody");
      if (!body) {
        return;
      }

      const key = header.dataset.sortKey;
      const direction = header.dataset.sortDirection === "asc" ? "desc" : "asc";
      header.dataset.sortDirection = direction;
      const rows = Array.from(body.querySelectorAll("tr"));
      rows.sort((left, right) => {
        const leftValue = left.dataset[key] ?? "";
        const rightValue = right.dataset[key] ?? "";
        const leftNumber = Number(leftValue);
        const rightNumber = Number(rightValue);
        const compare = Number.isNaN(leftNumber) || Number.isNaN(rightNumber)
          ? leftValue.localeCompare(rightValue)
          : leftNumber - rightNumber;
        return direction === "asc" ? compare : -compare;
      });
      rows.forEach((row) => body.appendChild(row));
    });
  });
}

function updateCustomIdPreview() {
  const preview = document.getElementById("custom-id-preview");
  const list = document.getElementById("custom-id-list");
  if (!preview || !list) {
    return;
  }

  const parts = Array.from(list.querySelectorAll(".builder-row")).map((row, index) => {
    const type = row.querySelector('select[name^="elements["]')?.value ?? "Fixed";
    const fixed = row.querySelector('input[name$=".FixedText"]')?.value ?? "";
    const format = row.querySelector('input[name$=".Format"]')?.value ?? "";
    return renderCustomIdPart(type, fixed, format, index + 13);
  });
  preview.textContent = parts.length ? parts.join("") : "ITEM-000013";
}

function renderCustomIdPart(type, fixed, format, sequence) {
  switch (type) {
    case "Fixed":
      return fixed;
    case "Random20Bit":
      return formatNumber(683241, format);
    case "Random32Bit":
      return formatNumber(3626764231, format);
    case "Random6Digit":
      return formatNumber(582914, format || "D6");
    case "Random9Digit":
      return formatNumber(829416703, format || "D9");
    case "Guid":
      return format?.toUpperCase() === "D" ? "82c21a56-640f-4e83-a242-b0b734ca2c33" : "82c21a56640f4e83a242b0b734ca2c33";
    case "DateTime":
      return formatDateExample(format || "yyyyMMdd");
    case "Sequence":
      return formatNumber(sequence, format);
    default:
      return "";
  }
}

function formatNumber(value, format) {
  if (!format) {
    return value.toString();
  }

  const width = Number(format.slice(1));
  if (format[0]?.toUpperCase() === "D" && width > 0) {
    return value.toString().padStart(width, "0");
  }

  if (format[0]?.toUpperCase() === "X") {
    const hex = value.toString(16).toUpperCase();
    return width > 0 ? hex.padStart(width, "0") : hex;
  }

  return value.toString();
}

function formatDateExample(format) {
  return format
    .replaceAll("yyyy", "2026")
    .replaceAll("yy", "26")
    .replaceAll("MM", "05")
    .replaceAll("dd", "15")
    .replaceAll("HH", "09")
    .replaceAll("mm", "30")
    .replaceAll("ss", "00");
}

function escapeHtml(value) {
  return value
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll("\"", "&quot;")
    .replaceAll("'", "&#39;");
}
