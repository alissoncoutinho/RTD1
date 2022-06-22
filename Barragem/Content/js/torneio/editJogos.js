var jogadoresDropDownList = [];


$(".date").datepicker({
    language: 'pt-BR',
    pickTime: false,
    locale: 'pt-br',
    dateFormat: 'dd/mm/yy'
});

$(document).ready(function () {
    $(".hora").mask("99:99");

    $(".inscricaoButton").click(function (event) {
        var i = $(this).data("valor");
        var Id = $("#form" + i).find("input[name=Id]").val();
        var jogador1 = document.getElementById("jogador1_" + i).value;
        var jogador2 = document.getElementById("jogador2_" + i).value;
        var dataJogo = document.getElementById("dataJogo" + i).value;
        var horaJogo = document.getElementById("horaJogo" + i).value;
        var quadra = document.getElementById("quadra" + i).value;
        event.preventDefault();
        $.ajax({
            type: "POST",
            url: "/Torneio/EditJogosV2",
            dataType: "json",
            data: "{'Id':'" + Id + "', 'jogador1':'" + jogador1 + "', 'jogador2':'" + jogador2 + "', 'dataJogo':'" + dataJogo + "', 'horaJogo':'" + horaJogo + "', 'quadra':'" + quadra + "'}",
            contentType: "application/json; charset=utf-8",
            success: function (response) {
                if (typeof response == "object") {
                    toastr.options = {
                        "positionClass": "toast-top-center"
                    }
                    if (response.retorno === 0) {
                        toastr.error(response.erro, "Erro");
                    } else {
                        toastr.success("Atualização realizada com sucesso.", "Aviso");
                        if (response.retorno === 2) {
                            location.reload(true);
                        }
                    }
                }
            },
            error: function (jqXHR, textStatus, errorThrown) {
                toastr.error(errorThrown, "Erro");
            }
        });
    });
});

/**Abertura do painel para geração de tabela */
function AbrirTabelaGeracaoJogos() {
    MostrarClassesJogos("GERACAO_TABELA");
}

/**Abertura do painel para exclusão de tabela de jogos*/
function AbrirTabelaJogosExclusao() {
    MostrarClassesJogos("EXCLUSAO");
}

/**
 * Gerencia o comportamento para mostrar o painel de exclusao ou geração de tabela
 */
function MostrarClassesJogos(acao) {
    var elementGeracaoTabelaJogos = document.getElementById("classes");
    var elementExclusaoJogos = document.getElementById("classesExclusao");

    if (acao == "EXCLUSAO") {
        elementGeracaoTabelaJogos.classList.remove("show");
        elementExclusaoJogos.classList.add("show");
    }
    else if (acao == "GERACAO_TABELA") {
        elementGeracaoTabelaJogos.classList.add("show");
        elementExclusaoJogos.classList.remove("show");
    }
    else {
        elementGeracaoTabelaJogos.classList.remove("show");
        elementExclusaoJogos.classList.remove("show");
    }
}

function gerarTabelas() {
    event.preventDefault();

    var idTorneio = document.getElementById('torneioId').value;

    document.getElementById("divClasseJogosPoucosJogadores").style.display = "none";

    ValidarPagamentoTorneio(idTorneio);
}

/**
 * Valida se a(s) classe(s) selecionada(s) possui jogos já gerados
 */
function ValidarJogosJaGerados(idTorneio, acao) {
    var classesSelecionadas = "";
    if (acao == "GERACAO_TABELA") {
        classesSelecionadas = $('input[name=classeIds]:checked').map(function (_, el) {
            return $(el).val();
        }).get();
    }
    else if (acao == "EXCLUSAO") {
        classesSelecionadas = $('input[name=idsClassesExclusao]:checked').map(function (_, el) {
            return $(el).val();
        }).get();
    }

    $.ajax({
        type: "GET",
        url: "/Torneio/ValidarJogosJaGerados",
        dataType: "json",
        data: {
            "classeIds": classesSelecionadas,
            "torneioId": idTorneio
        },
        traditional: true,
        contentType: "application/json; charset=utf-8",
        success: function (response) {
            if (typeof response == "object") {
                toastr.options = {
                    "positionClass": "toast-top-center"
                }
                if (response.retorno == "ERRO") {
                    toastr.error(response.erro, "Erro");
                } else {
                    if (response.retorno != "OK") {
                        ExibirMsgConfirmaExclusaoJogos(response.retorno, acao);
                    }
                    else {
                        ExecutarAcaoTabelaJogos(acao);
                    }
                }
            }
        },
        error: function (jqXHR, textStatus, errorThrown) {
            toastr.error(errorThrown, "Erro");
        }
    });
}

/**
 * Valida se o pagamento do torneio foi efetuado
 */
function ValidarPagamentoTorneio(idTorneio) {
    $.ajax({
        type: "GET",
        url: "/Torneio/ValidarPagamentoTorneio",
        dataType: "json",
        data: {
            "torneioId": document.getElementById('torneioId').value
        },
        traditional: true,
        contentType: "application/json; charset=utf-8",
        success: function (response) {
            if (typeof response == "object") {
                toastr.options = {
                    "positionClass": "toast-top-center"
                }
                if (response.retorno == "ERRO") {
                    toastr.error(response.erro, "Erro");
                } else {
                    if (response.status == "PENDENCIA_CADASTRO") {
                        //SOLICITAR PREENCHIMENTO DADOS FALTANTES
                        document.getElementById("divPagamentoPendenteTorneio").style.display = "block";
                        document.getElementById("divPixPagtoTorneio").style.display = "none";
                        document.getElementById("divDadosCadastraisPendentesPagto").style.display = "block";

                        document.getElementById("txtNomePagador").value = response.retorno.Nome;
                        document.getElementById("txtCpfCnpjPagador").value = response.retorno.CpfCnpj;

                    }
                    else if (response.status == "PENDENCIA_PAGAMENTO" || response.status == "ERRO_QRCODE") {
                        //SOLICITAR PAGAMENTO
                        document.getElementById("divPagamentoPendenteTorneio").style.display = "block";
                        document.getElementById("divPixPagtoTorneio").style.display = "block";
                        document.getElementById("divDadosCadastraisPendentesPagto").style.display = "none";

                        document.getElementById("lblQtdeInscritosPagtoPend").textContent = "Inscritos no torneio: " + response.retorno.qtddInscritos;

                        if (response.retorno.valorDescontoParaRanking > 0) {
                            document.getElementById("lblDescRankingPagtoPend").textContent = "Desconto para usuários que jogam o ranking: R$ " + response.retorno.valorDescontoParaRanking + ",00";
                            document.getElementById("divDescRankingPagtoPend").style.display = "block";
                        }
                        else {
                            document.getElementById("divDescRankingPagtoPend").style.display = "none";
                            document.getElementById("lblDescRankingPagtoPend").textContent = "";
                        }
                        document.getElementById("lblValorFinalPagtoPend").textContent = "Valor final a pagar: " + response.retorno.valorASerPago + ",00";

                        if (response.status == "ERRO_QRCODE") {
                            document.getElementById("lblDescErroPagtoPend").textContent = response.retorno.qrCode.erroGerarQrCode;
                            document.getElementById("divErroQrCodePagtoPend").style.display = "block";
                            document.getElementById("divQrCodePagtoPend").style.display = "none";
                        }
                        else {
                            document.getElementById("imgQrCodePagtoPend").src = response.retorno.qrCode.link;
                            document.getElementById("lblTextoQrCodePagtoPend").textContent = response.retorno.qrCode.text;
                            document.getElementById("divErroQrCodePagtoPend").style.display = "none";
                            document.getElementById("divQrCodePagtoPend").style.display = "block";
                        }
                    }
                    else {
                        document.getElementById("divPagamentoPendenteTorneio").style.display = "none";
                        document.getElementById("divClasseJogosPoucosJogadores").style.display = "none";
                        if ($("#chkTemClassesMenosSeisJogadores").val() == "N") {
                            ValidarJogosJaGerados(idTorneio, "GERACAO_TABELA");
                        }
                        else {
                            ExibirAlteracaoClassesGeracaoJogos();
                        }
                    }
                }
            }
        },
        error: function (jqXHR, textStatus, errorThrown) {
            toastr.error(errorThrown, "Erro");
        }
    });
}

/**Executa a exclusão de tabela de jogos */
function ExcluirTabelaJogos() {
    var idTorneio = document.getElementById('torneioId').value;
    ValidarJogosJaGerados(idTorneio, "EXCLUSAO");
}

/*Salva as alterações de config. das Classes e efetua a geração dos jogos*/
function SalvarAlteracaoClassesGeracaoJogos() {
    $.ajax({
        type: "POST",
        url: "/Torneio/SalvarAlteracaoClassesGeracaoJogos",
        dataType: "json",
        data: $("#FormAlteracoesClassesGeracaoJogos").serialize(),
        traditional: true,
        contentType: "application/x-www-form-urlencoded; charset=UTF-8",
        success: function (response) {
            if (typeof response == "object") {
                toastr.options = {
                    "positionClass": "toast-top-center"
                }
                if (response.status == "ERRO") {
                    toastr.error(response.erro, "Erro");
                } else {
                    toastr.success(response.mensagem, "Sucesso");
                    document.getElementById("divClasseJogosPoucosJogadores").style.display = "none";
                    var idTorneio = document.getElementById('torneioId').value;
                    ValidarJogosJaGerados(idTorneio, "GERACAO_TABELA");
                }
            }
        },
        error: function (jqXHR, textStatus, errorThrown) {
            toastr.error(errorThrown, "Erro");
        }
    });
}

function ExibirAlteracaoClassesGeracaoJogos() {
    if (ValidarExistenciaClasseMenosSeisInscritos() == true) {
        document.getElementById("divClasseJogosPoucosJogadores").style.display = "block";
    }
    else {
        document.getElementById("divClasseJogosPoucosJogadores").style.display = "none";
        var idTorneio = document.getElementById('torneioId').value;
        ValidarJogosJaGerados(idTorneio, "GERACAO_TABELA");
    }
}

function ValidarExistenciaClasseMenosSeisInscritos() {
    var classesMenosSeisJogadores = $('input[name=classeIdsMenosSeisJogadores]').map(function (_, el) {
        return $(el).val();
    }).get();

    var classesSelecionadas = $('input[name=classeIds]:checked').map(function (_, el) {
        return $(el).val();
    }).get();

    var temClassesPoucosJogadores = false;

    for (var i = 0; i < classesMenosSeisJogadores.length; i++) {
        if (classesSelecionadas.indexOf(classesMenosSeisJogadores[i]) > -1) {
            document.getElementById("div_classeitem_" + classesMenosSeisJogadores[i]).style.display = "block";
            document.getElementById("lbl_classeitem_" + classesMenosSeisJogadores[i]).style.display = "block";
            temClassesPoucosJogadores = true;
        }
        else {
            document.getElementById("div_classeitem_" + classesMenosSeisJogadores[i]).style.display = "none";
            document.getElementById("lbl_classeitem_" + classesMenosSeisJogadores[i]).style.display = "none";
        }
    }
    return temClassesPoucosJogadores;
}

/**Salvar dados cadastrais pendentes para pagamento do torneio */
function SalvarDadosCadastraisPendentesPagto() {
    var nome = document.getElementById("txtNomePagador").value;
    var cpfCnpj = document.getElementById("txtCpfCnpjPagador").value;
    var torneioId = document.getElementById('torneioId').value;

    toastr.options = {
        "positionClass": "toast-top-center"
    }

    if (nome == "") {
        toastr.error("O campo Nome é obrigatório", "Erro");
        return;
    }
    else if (cpfCnpj == "") {
        toastr.error("O campo CPF/CNPJ é obrigatório", "Erro");
        return;
    }

    $.ajax({
        type: "POST",
        url: "/Torneio/SalvarDadosCadastraisPendentesPagto",
        dataType: "json",
        data: "{'torneioId':'" + torneioId + "', " + "'nome':'" + nome + "', " + "'cpfCnpj':'" + cpfCnpj + "'}",
        traditional: true,
        contentType: "application/json; charset=utf-8",
        success: function (response) {
            if (typeof response == "object") {
                toastr.options = {
                    "positionClass": "toast-top-center"
                }
                if (response.status == "ERRO") {
                    toastr.error(response.erro, "Erro");
                } else {
                    ValidarPagamentoTorneio(torneioId);
                }
            }
        },
        error: function (jqXHR, textStatus, errorThrown) {
            toastr.error(errorThrown, "Erro");
        }
    });
}

function FecharPixPagtoTorneio() {
    document.getElementById("divPixPagtoTorneio").style.display = "none";
}

function FecharDadosCadastraisPendentesPagto() {
    document.getElementById("divDadosCadastraisPendentesPagto").style.display = "none";
}

/**
 * Exibe mensagem de confirmação da exclusão de jogos com tabelas existente
 */
function ExibirMsgConfirmaExclusaoJogos(conteudoMensagem, acao) {

    var textoInicial = "";

    if (acao == "GERACAO_TABELA") {
        textoInicial = "<p>Você selecionou categorias que já tem jogos concluídos ou horários marcados. <b>Deseja substituir as tabelas já existentes? </b></p>Categorias cujas tabelas serão zeradas:</br>";
    }
    else if (acao == "EXCLUSAO") {
        textoInicial = "<p>Você selecionou categorias que já tem jogos concluídos ou horários marcados. <b>Deseja EXCLUIR as tabelas? </b></p>Categorias com jogos que serão EXCLUÍDAS:</br>";

    }

    $.confirm({
        title: "<span style='color:red'>ATENÇÃO!!!</span>",
        content: textoInicial + conteudoMensagem,
        buttons: {
            sim: {
                text: 'Sim',
                btnClass: 'btn-primary',
                action: function () {
                    ExecutarAcaoTabelaJogos(acao);
                }
            },
            cancelar: {}
        }
    });
}

/**
 * Submete o formulário correspontente a ação executada
 */
function ExecutarAcaoTabelaJogos(acao) {
    if (acao == "EXCLUSAO") {
        document.forms["FormClassesExclusao"].submit();
    }
    else if (acao == "GERACAO_TABELA") {
        document.forms["FormClasses"].submit();
    }
    else {
        return;
    }
}

/**Marcar classes para geração de tabela de jogos */
function marcarTodos() {
    var isChecked = false;
    if (document.getElementById('todos').checked) {
        isChecked = true;
    }
    var checkboxes = document.getElementsByName("classeIds");
    for (var i = 0; i < checkboxes.length; i++) {
        if (checkboxes[i].type == "checkbox") {
            checkboxes[i].checked = isChecked;
        }
    }
}

/**Marcar classes para exclusão de tabela de jogos */
function marcarTodosJogosExclusao() {
    var isChecked = false;
    if (document.getElementById('chkExcluirTodasClasses').checked) {
        isChecked = true;
    }
    var checkboxes = document.getElementsByName("idsClassesExclusao");
    for (var i = 0; i < checkboxes.length; i++) {
        if (checkboxes[i].type == "checkbox") {
            checkboxes[i].checked = isChecked;
        }
    }
}

function Pesquisar() {
    var torneioId = document.getElementById('torneioId').value;
    var classeId = document.getElementById('fClasse').value;
    var data = document.getElementById('fData').value;
    var nomeJogador = document.getElementById('fNomeJogador').value;
    var grupo = document.getElementById('fGrupo').value;
    var fase = document.getElementById('fase').value;
    window.location = "EditJogos?torneioId=" + torneioId + "&fClasse=" + classeId + "&fData=" + data + "&fNomeJogador=" + nomeJogador + "&fGrupo=" + grupo + "&fase=" + fase + "&nomeJogador=" + nomeJogador;
}
function Imprimir() {
    var torneioId = document.getElementById('torneioId').value;
    var classeId = document.getElementById('fClasse').value;
    var data = document.getElementById('fData').value;
    var nomeJogador = document.getElementById('fNomeJogador').value;
    var grupo = document.getElementById('fGrupo').value;
    var fase = document.getElementById('fase').value;
    window.open("ImprimirJogos?torneioId=" + torneioId + "&fClasse=" + classeId + "&fData=" + data + "&fNomeJogador=" + nomeJogador + "&fGrupo=" + grupo + "&fase=" + fase, '_blank');
}

function CarregarJogadores() {
    $.get('/torneio/ObterJogadores?torneioId=' + document.getElementById('torneioId').value + "&classeId=" + document.getElementById('fClasse').value + "&grupoId=" + document.getElementById('fGrupo').value,
        function (data, status) {
            CarregarOpcoesJogador(data);
        });
}

function CarregarOpcoesJogador(dadosJogadores) {

    var dropdownsJogadores = document.getElementsByClassName("escolhaJogador");

    for (var i = 0; i < dropdownsJogadores.length; i++) {
        //obtém opções de jogadores para carregar cada combo
        var opcoesDisponiveis = FiltrarOpcoesJogadores(dadosJogadores, dropdownsJogadores[i].dataset.jogoid, dropdownsJogadores[i].dataset.tipojogador);

        //Cria combos e mantem instancias na lista jogadoresDropDownList
        jogadoresDropDownList.push(
            [
                dropdownsJogadores[i],
                jSuites.dropdown(dropdownsJogadores[i], {
                    data: opcoesDisponiveis,
                    width: 'auto',
                    autocomplete: true,
                    onopen: function (el) {
                        for (var i = 0; i < jogadoresDropDownList.length; i++) {
                            var dropItem = jogadoresDropDownList[i][1];
                            if (el.id != jogadoresDropDownList[i][0].id && dropItem.options.opened == true) {
                                dropItem.close();
                            }
                            else {
                                dropItem.options.opened = true;
                            }
                        }
                    },
                })
            ]
        );

        //Seleciona jogador no combo
        jogadoresDropDownList[i][1].setValue(jogadoresDropDownList[i][0].dataset.jogador)
    }
}

function FiltrarOpcoesJogadores(dadosJogadores, idJogo, tipojogador) {

    //1) Regra para não exibir jogador adversário
    var dadosJogo = dadosJogadores.Jogos.find(jogo => jogo.JogoId == idJogo);
    if (dadosJogo != null) {

        var copiaOpcoesJogador = dadosJogadores.OpcoesJogador.slice();
        var copiaOpcoesJogadorMataMata = dadosJogadores.OpcoesJogadorMataMata.slice();

        var jogoMataMata = dadosJogo.Grupo == null;

        if (jogoMataMata) {
            var idJogador = ObterIdJogador(copiaOpcoesJogadorMataMata, dadosJogo, tipojogador);
            var i = copiaOpcoesJogadorMataMata.length;
            while (i--) {
                if (idJogador == 0 && copiaOpcoesJogadorMataMata[i].value == 10)
                {
                    //Manter somente a opção Aguardando adversário
                    copiaOpcoesJogadorMataMata.splice(i, 1);
                }
                else if (idJogador > 0 && copiaOpcoesJogadorMataMata[i].value == 0) {
                    //3) Remover opção Aguardando adversário caso tenha algum jogador já selecionado
                    copiaOpcoesJogadorMataMata.splice(i, 1);
                }
                else if (idJogador > 0 && idJogador != 10 && copiaOpcoesJogadorMataMata[i].value == 10) {
                    //3) Remover opção Bye caso tenha algum jogador já selecionado
                    copiaOpcoesJogadorMataMata.splice(i, 1);
                }
                else if (idJogador != copiaOpcoesJogadorMataMata[i].value && copiaOpcoesJogadorMataMata[i].JogadorAlocado && dadosJogo.Grupo != null && dadosJogo.Grupo == copiaOpcoesJogador[i].Grupo) {
                    copiaOpcoesJogadorMataMata.splice(i, 1);
                }
                else if (tipojogador == "desafiante" && copiaOpcoesJogadorMataMata[i].value != 0 && copiaOpcoesJogadorMataMata[i].value != 10) {
                    if (copiaOpcoesJogadorMataMata[i].value == dadosJogo.IdDesafiado && dadosJogo.IdDesafiado != 10 && dadosJogo.IdDesafiado != 0) {
                        copiaOpcoesJogadorMataMata.splice(i, 1);
                    }
                }
                else if (tipojogador == "desafiado" && copiaOpcoesJogadorMataMata[i].value != 0) {
                    if (copiaOpcoesJogadorMataMata[i].value == dadosJogo.IdDesafiante && dadosJogo.IdDesafiante != 10 && dadosJogo.IdDesafiante != 0) {
                        copiaOpcoesJogadorMataMata.splice(i, 1);
                    }
                }

            }

            return copiaOpcoesJogadorMataMata;
        }
        else {
            var idJogador = ObterIdJogador(copiaOpcoesJogador, dadosJogo, tipojogador);
            var i = copiaOpcoesJogador.length;
            while (i--) {

                if (dadosJogo.Grupo == null && copiaOpcoesJogador[i].group != "  " && copiaOpcoesJogador[i].group != "FORA DA TABELA") {
                    //Quando for mata-mata altera nome para NA TABELA
                    copiaOpcoesJogador[i].group = "NA TABELA";
                }

                if (idJogador > 0 && copiaOpcoesJogador[i].value == 0) {
                    //3) Remover opção Aguardando adversário caso tenha algum jogador já selecionado
                    copiaOpcoesJogador.splice(i, 1);
                }
                else if (idJogador > 0 && idJogador != 10 && copiaOpcoesJogador[i].value == 10) {
                    //3) Remover opção Bye caso tenha algum jogador já selecionado
                    copiaOpcoesJogador.splice(i, 1);
                }
                else if (dadosJogadores.EhGrupoUnico) {
                    //2) Regra exibir apenas jogadores FORA DA TABELA e bye
                    if (copiaOpcoesJogador[i].group != "FORA DA TABELA" && copiaOpcoesJogador[i].value != 10 && copiaOpcoesJogador[i].value != idJogador) {
                        copiaOpcoesJogador.splice(i, 1);
                    }
                }
                else if (idJogador != copiaOpcoesJogador[i].value && copiaOpcoesJogador[i].JogadorAlocado && dadosJogo.Grupo != null && dadosJogo.Grupo == copiaOpcoesJogador[i].Grupo) {
                    copiaOpcoesJogador.splice(i, 1);
                }
                else if (tipojogador == "desafiante" && copiaOpcoesJogador[i].value != 0) {
                    if (copiaOpcoesJogador[i].value == dadosJogo.IdDesafiado && dadosJogo.IdDesafiado != 10 && dadosJogo.IdDesafiado != 0) {
                        copiaOpcoesJogador.splice(i, 1);
                    }
                }
                else if (tipojogador == "desafiado" && copiaOpcoesJogador[i].value != 0) {
                    if (copiaOpcoesJogador[i].value == dadosJogo.IdDesafiante && dadosJogo.IdDesafiante != 10 && dadosJogo.IdDesafiante != 0) {
                        copiaOpcoesJogador.splice(i, 1);
                    }
                }
            }

            return copiaOpcoesJogador;
        }
    }
}

function ObterIdJogador(opcoesJogador, jogo, tipojogador) {
    if (tipojogador == "desafiante") {
        var jogador = opcoesJogador.find(x => x.value == jogo.IdDesafiante);
        if (jogador != null) {
            return jogador.value;
        }
        return 0;
    }
    else {
        var jogador = opcoesJogador.find(x => x.value == jogo.IdDesafiado);
        if (jogador != null) {
            return jogador.value;
        }
        return 0;
    }
}

function ObterNomeJogadorSelecionado(indice, tipoJogador) {
    for (var i = 0; i < jogadoresDropDownList.length; i++) {
        var dropItem = jogadoresDropDownList[i][1];
        if (indice == jogadoresDropDownList[i][0].dataset.indice && tipoJogador == jogadoresDropDownList[i][0].dataset.tipojogador) {
            return dropItem.getText();
        }
    }
    return null;
}

$(document).mouseup(function (el) {
    if (el.target.classList.contains("jdropdown-header") == false && el.target.classList.contains("jdropdown-content") == false) {
        for (var i = 0; i < jogadoresDropDownList.length; i++) {
            var dropdownItem = jogadoresDropDownList[i][1];
            if (el.id != jogadoresDropDownList[i][0].id && dropdownItem.options.opened == true) {
                dropdownItem.close();
            }
            else {
                dropdownItem.options.opened = true;
            }
        }
    }
});

CarregarJogadores();
