document.addEventListener('DOMContentLoaded', () => {
    const form = document.getElementById('receiptUploadForm');
    if (form) {
        const fileInput = document.getElementById('receiptFileInput');
        const rawTextInput = document.getElementById('clientOcrRawText');
        const statusText = document.getElementById('ocrStatusText');
        const submitBtn = document.getElementById('extractSubmitBtn');

        if (fileInput && rawTextInput && statusText && submitBtn) {
            fileInput.addEventListener('change', () => {
                delete form.dataset.ocrDone;
                rawTextInput.value = '';
                statusText.hidden = true;
            });

            form.addEventListener('submit', async (event) => {
                if (form.dataset.ocrDone === 'true' || typeof Tesseract === 'undefined') {
                    return;
                }

                if (window.jQuery && !window.jQuery(form).valid()) {
                    return;
                }

                const file = fileInput.files && fileInput.files[0];
                if (!file || !file.type.startsWith('image/')) {
                    return;
                }

                event.preventDefault();
                submitBtn.disabled = true;
                submitBtn.textContent = 'Scanning receipt...';
                statusText.hidden = false;
                statusText.textContent = 'Running OCR in browser (first scan downloads the English language model)...';

                try {
                    const result = await Tesseract.recognize(file, 'eng', {
                        logger: (msg) => {
                            if (msg.status === 'recognizing text') {
                                const pct = Math.round((msg.progress || 0) * 100);
                                statusText.textContent = `Scanning receipt... ${pct}%`;
                            }
                        }
                    });
                    rawTextInput.value = result?.data?.text || '';
                } catch {
                    rawTextInput.value = '';
                } finally {
                    form.dataset.ocrDone = 'true';
                    form.submit();
                }
            });
        }
    }

    const chatRoot = document.getElementById('fxChatbot');
    if (chatRoot) {
        const chatWindow = document.getElementById('fxChatWindow');
        const toggleBtn = document.getElementById('fxChatToggle');
        const closeBtn = document.getElementById('fxChatClose');
        const messagesEl = document.getElementById('fxChatMessages');
        const promptsEl = document.getElementById('fxChatPrompts');
        const chatForm = document.getElementById('fxChatForm');
        const chatInput = document.getElementById('fxChatInput');
        const sendBtn = document.getElementById('fxChatSend');

        const promptsUrl = chatRoot.dataset.promptsUrl || '/Chatbot/Prompts';
        const askUrl = chatRoot.dataset.askUrl || '/Chatbot/Ask';
        let promptsLoaded = false;

        const formatChatHtml = (text) => {
            const escaped = String(text || '')
                .replace(/&/g, '&amp;')
                .replace(/</g, '&lt;')
                .replace(/>/g, '&gt;');
            const withBold = escaped.replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>');
            const lines = withBold.split(/\r?\n/);
            let html = '';
            let inList = false;
            const closeList = () => {
                if (inList) {
                    html += '</ul>';
                    inList = false;
                }
            };
            lines.forEach((line) => {
                const bullet = line.match(/^\s*[-•]\s+(.+)$/);
                if (bullet) {
                    if (!inList) {
                        html += '<ul class="fx-chat__list">';
                        inList = true;
                    }
                    html += `<li>${bullet[1]}</li>`;
                    return;
                }
                closeList();
                if (line.trim() === '') {
                    html += '<div class="fx-chat__break"></div>';
                    return;
                }
                html += `<p class="fx-chat__p">${line}</p>`;
            });
            closeList();
            return html || withBold;
        };

        const appendMessage = (text, sender) => {
            const bubble = document.createElement('div');
            bubble.className = `fx-chat__msg fx-chat__msg--${sender}`;
            if (sender === 'bot') {
                bubble.innerHTML = formatChatHtml(text);
            } else {
                bubble.textContent = text;
            }
            messagesEl.appendChild(bubble);
            messagesEl.scrollTop = messagesEl.scrollHeight;
            return bubble;
        };

        const renderPrompts = (prompts) => {
            promptsEl.innerHTML = '';
            if (!Array.isArray(prompts) || prompts.length === 0) {
                return;
            }
            prompts.slice(0, 3).forEach((promptText) => {
                const chip = document.createElement('button');
                chip.type = 'button';
                chip.className = 'fx-chat__chip';
                chip.textContent = promptText;
                chip.addEventListener('click', () => sendMessage(promptText));
                promptsEl.appendChild(chip);
            });
        };

        const loadInitialPrompts = async () => {
            if (promptsLoaded) {
                return;
            }
            promptsLoaded = true;
            try {
                const res = await fetch(promptsUrl, { headers: { Accept: 'application/json' } });
                if (res.ok) {
                    const data = await res.json();
                    renderPrompts(data.suggestedPrompts || data.SuggestedPrompts || []);
                }
            } catch {
            }
        };

        const setOpen = (isOpen) => {
            chatWindow.hidden = !isOpen;
            chatRoot.classList.toggle('is-open', isOpen);
            toggleBtn.setAttribute('aria-expanded', String(isOpen));
            if (isOpen) {
                loadInitialPrompts();
                chatInput.focus();
            }
        };

        const sendMessage = async (rawMessage) => {
            const message = rawMessage.trim();
            if (!message) {
                return;
            }

            appendMessage(message, 'user');
            chatInput.value = '';
            chatInput.disabled = true;
            sendBtn.disabled = true;

            const typingBubble = appendMessage('Thinking...', 'bot');

            try {
                const res = await fetch(askUrl, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        Accept: 'application/json'
                    },
                    body: JSON.stringify({ message })
                });

                const data = await res.json();
                const reply = data.reply || data.Reply || 'Sorry, I could not generate a response.';
                typingBubble.innerHTML = formatChatHtml(reply);

                const nextPrompts = data.suggestedPrompts || data.SuggestedPrompts;
                if (Array.isArray(nextPrompts) && nextPrompts.length > 0) {
                    renderPrompts(nextPrompts);
                }
            } catch {
                typingBubble.innerHTML = formatChatHtml('Unable to reach the assistant right now. Please try again.');
            } finally {
                chatInput.disabled = false;
                sendBtn.disabled = false;
                chatInput.focus();
            }
        };

        toggleBtn.addEventListener('click', () => setOpen(chatWindow.hidden));
        closeBtn.addEventListener('click', () => setOpen(false));
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape' && !chatWindow.hidden) {
                setOpen(false);
            }
        });

        chatForm.addEventListener('submit', (e) => {
            e.preventDefault();
            sendMessage(chatInput.value);
        });
    }
});