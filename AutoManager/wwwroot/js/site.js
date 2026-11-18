// AutoManager — interações da interface.
// Este arquivo apenas melhora a usabilidade: todas as regras e validações
// também são aplicadas no servidor.
(function () {
    "use strict";

    // Menu lateral no celular -------------------------------------------------
    function initSidebar() {
        const body = document.body;
        document.querySelectorAll("[data-am-toggle-sidebar]").forEach(function (btn) {
            btn.addEventListener("click", function () {
                const aberto = body.classList.toggle("am-sidebar-open");
                btn.setAttribute("aria-expanded", aberto ? "true" : "false");
            });
        });
        document.querySelectorAll("[data-am-close-sidebar]").forEach(function (el) {
            el.addEventListener("click", function () { body.classList.remove("am-sidebar-open"); });
        });
        document.addEventListener("keydown", function (e) {
            if (e.key === "Escape") body.classList.remove("am-sidebar-open");
        });
    }

    // Mostrar/ocultar senha -----------------------------------------------------
    function initPassword() {
        document.querySelectorAll("[data-am-password-toggle]").forEach(function (btn) {
            btn.addEventListener("click", function () {
                const input = document.getElementById(btn.getAttribute("data-am-password-toggle"));
                if (!input) return;
                const mostrar = input.type === "password";
                input.type = mostrar ? "text" : "password";
                btn.setAttribute("aria-label", mostrar ? "Ocultar senha" : "Mostrar senha");
                const icone = btn.querySelector("i");
                if (icone) icone.className = mostrar ? "bi bi-eye-slash" : "bi bi-eye";
            });
        });
    }

    // Máscara de telefone: (16) 99999-9999 ou (16) 3333-4444 -------------------
    function formatarTelefone(valor) {
        const d = valor.replace(/\D/g, "").slice(0, 11);
        if (d.length === 0) return "";
        if (d.length <= 2) return "(" + d;
        if (d.length <= 6) return "(" + d.slice(0, 2) + ") " + d.slice(2);
        if (d.length <= 10) return "(" + d.slice(0, 2) + ") " + d.slice(2, 6) + "-" + d.slice(6);
        return "(" + d.slice(0, 2) + ") " + d.slice(2, 7) + "-" + d.slice(7);
    }

    function initTelefone() {
        document.querySelectorAll("[data-am-mask='telefone']").forEach(function (input) {
            input.value = formatarTelefone(input.value);
            input.addEventListener("input", function () { input.value = formatarTelefone(input.value); });
        });
    }

    // Placa: maiúsculas, apenas letras/números, hífen opcional --------------------
    function initPlaca() {
        document.querySelectorAll("[data-am-mask='placa']").forEach(function (input) {
            input.addEventListener("input", function () {
                const pos = input.selectionStart;
                input.value = input.value.toUpperCase().replace(/[^A-Z0-9-]/g, "").slice(0, 8);
                if (pos !== null) input.setSelectionRange(pos, pos);
            });
        });
    }

    // Filtro instantâneo de listas/tabelas ----------------------------------------
    function normalizar(texto) {
        return (texto || "").toString().toLowerCase()
            .normalize("NFD").replace(/[̀-ͯ]/g, "")
            .replace(/[-\s()]/g, "");
    }

    function initFiltro() {
        document.querySelectorAll("[data-am-filter]").forEach(function (input) {
            const alvo = document.querySelector(input.getAttribute("data-am-filter"));
            if (!alvo) return;
            const vazio = document.querySelector(input.getAttribute("data-am-filter-empty") || "#__nada");
            const aplicar = function () {
                const termo = normalizar(input.value);
                let visiveis = 0;
                alvo.querySelectorAll("[data-am-filter-item]").forEach(function (item) {
                    const texto = normalizar(item.getAttribute("data-am-filter-item") || item.textContent);
                    const mostrar = termo === "" || texto.indexOf(termo) !== -1;
                    item.classList.toggle("d-none", !mostrar);
                    if (mostrar) visiveis++;
                });
                if (vazio) vazio.classList.toggle("d-none", visiveis !== 0);
            };
            input.addEventListener("input", aplicar);
            aplicar();
        });
    }

    // Linhas de tabela clicáveis ------------------------------------------------
    function initLinhasClicaveis() {
        document.querySelectorAll("tr[data-am-href]").forEach(function (tr) {
            tr.classList.add("row-link");
            tr.addEventListener("click", function (e) {
                if (e.target.closest("a, button, input, form, label")) return;
                window.location.href = tr.getAttribute("data-am-href");
            });
        });
    }

    // Contador de caracteres -------------------------------------------------------
    function initContador() {
        document.querySelectorAll("[data-am-count]").forEach(function (campo) {
            const saida = document.getElementById(campo.getAttribute("data-am-count"));
            const max = parseInt(campo.getAttribute("maxlength") || "0", 10);
            if (!saida) return;
            const atualizar = function () {
                saida.textContent = campo.value.length + (max ? " / " + max : "");
            };
            campo.addEventListener("input", atualizar);
            atualizar();
        });
    }

    // Evita envio duplo de formulários -------------------------------------------
    function initEnvioUnico() {
        document.querySelectorAll("form[data-am-once]").forEach(function (form) {
            form.addEventListener("submit", function (e) {
                if (window.jQuery && jQuery(form).valid && !jQuery(form).valid()) return;
                if (form.dataset.enviado === "1") { e.preventDefault(); return; }
                form.dataset.enviado = "1";
                const botao = e.submitter;
                if (botao) {
                    // Mantém o valor do botão clicado (ex.: "salvar e continuar")
                    if (botao.name) {
                        const oculto = document.createElement("input");
                        oculto.type = "hidden"; oculto.name = botao.name; oculto.value = botao.value;
                        form.appendChild(oculto);
                    }
                    setTimeout(function () { botao.disabled = true; }, 0);
                }
            });
        });
    }

    document.addEventListener("DOMContentLoaded", function () {
        initSidebar();
        initPassword();
        initTelefone();
        initPlaca();
        initFiltro();
        initLinhasClicaveis();
        initContador();
        initEnvioUnico();
    });
})();
