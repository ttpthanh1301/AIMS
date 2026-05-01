(function () {
    function escapeHtml(value) {
        return (value || '')
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#039;');
    }

    function renderInline(value) {
        let html = value;
        html = html.replace(/!\[([^\]]*)\]\((https?:\/\/[^)]+|\/[^)]+)\)/g, '<img src="$2" alt="$1" />');
        html = html.replace(/\[([^\]]+)\]\((https?:\/\/[^)]+|\/[^)]+)\)/g, '<a href="$2" target="_blank" rel="noopener">$1</a>');
        html = html.replace(/\*\*([^*]+)\*\*/g, '<strong>$1</strong>');
        html = html.replace(/\*([^*]+)\*/g, '<em>$1</em>');
        html = html.replace(/`([^`]+)`/g, '<code>$1</code>');
        return html;
    }

    function renderTable(lines, startIndex) {
        if (startIndex + 1 >= lines.length) {
            return null;
        }

        const header = lines[startIndex];
        const separator = lines[startIndex + 1];
        const hasMarkdownSeparator = header.includes('|') && /^\s*\|?\s*:?-{3,}:?\s*(\|\s*:?-{3,}:?\s*)+\|?\s*$/.test(separator);
        const hasPipeRows = header.includes('|') && separator.includes('|');
        const hasTabRows = header.includes('\t') && separator.includes('\t');

        if (!hasMarkdownSeparator && !hasPipeRows && !hasTabRows) {
            return null;
        }

        const readCells = line => {
            if (hasTabRows) {
                return line.split('\t').map(cell => renderInline(cell.trim()));
            }

            return line
                .trim()
                .replace(/^\|/, '')
                .replace(/\|$/, '')
                .split('|')
                .map(cell => renderInline(cell.trim()));
        };

        const headers = readCells(header);
        const rows = [];
        let index = startIndex + (hasMarkdownSeparator ? 2 : 1);

        while (index < lines.length && (hasTabRows ? lines[index].includes('\t') : lines[index].includes('|'))) {
            rows.push(readCells(lines[index]));
            index++;
        }

        const thead = `<thead><tr>${headers.map(cell => `<th>${cell}</th>`).join('')}</tr></thead>`;
        const tbody = `<tbody>${rows.map(row => `<tr>${row.map(cell => `<td>${cell}</td>`).join('')}</tr>`).join('')}</tbody>`;
        return {
            html: `<div class="task-markdown-table-wrap"><table class="task-markdown-table">${thead}${tbody}</table></div>`,
            nextIndex: index
        };
    }

    function render(markdown) {
        const escaped = escapeHtml(markdown || '');
        const lines = escaped.split(/\r?\n/);
        const blocks = [];
        let index = 0;

        while (index < lines.length) {
            const line = lines[index];

            if (!line.trim()) {
                index++;
                continue;
            }

            const table = renderTable(lines, index);
            if (table) {
                blocks.push(table.html);
                index = table.nextIndex;
                continue;
            }

            if (/^&gt;\s+/.test(line)) {
                const quoteLines = [];
                while (index < lines.length && /^&gt;\s+/.test(lines[index])) {
                    quoteLines.push(lines[index].replace(/^&gt;\s+/, ''));
                    index++;
                }
                blocks.push(`<blockquote>${quoteLines.map(renderInline).join('<br>')}</blockquote>`);
                continue;
            }

            if (/^\s*-\s+/.test(line)) {
                const items = [];
                while (index < lines.length && /^\s*-\s+/.test(lines[index])) {
                    items.push(lines[index].replace(/^\s*-\s+/, ''));
                    index++;
                }
                blocks.push(`<ul>${items.map(item => `<li>${renderInline(item)}</li>`).join('')}</ul>`);
                continue;
            }

            if (/^\s*\d+\.\s+/.test(line)) {
                const items = [];
                while (index < lines.length && /^\s*\d+\.\s+/.test(lines[index])) {
                    items.push(lines[index].replace(/^\s*\d+\.\s+/, ''));
                    index++;
                }
                blocks.push(`<ol>${items.map(item => `<li>${renderInline(item)}</li>`).join('')}</ol>`);
                continue;
            }

            const paragraph = [];
            while (
                index < lines.length &&
                lines[index].trim() &&
                !renderTable(lines, index) &&
                !/^&gt;\s+/.test(lines[index]) &&
                !/^\s*-\s+/.test(lines[index]) &&
                !/^\s*\d+\.\s+/.test(lines[index])
            ) {
                paragraph.push(lines[index]);
                index++;
            }
            blocks.push(`<p>${paragraph.map(renderInline).join('<br>')}</p>`);
        }

        return blocks.join('');
    }

    function hydrate(root) {
        const scope = root || document;
        scope.querySelectorAll('[data-task-markdown]').forEach(element => {
            element.innerHTML = render(element.textContent || '');
            element.removeAttribute('data-task-markdown');
        });
    }

    window.taskMarkdown = { render, hydrate };
    document.addEventListener('DOMContentLoaded', () => hydrate(document));
})();
