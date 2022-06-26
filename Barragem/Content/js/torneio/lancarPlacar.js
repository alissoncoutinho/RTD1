var loaderPage = $('#loadingDiv');
var loaderPageConsolidacao = $('#loadingConsolidacaoDiv');

var LoaderSelecionado;
const TipoLoader = {
    PADRAO: 1,
    CONSOLIDACAO: 2
};

function LancarPlacar(el, origem) {
    ShowLoader(false);
    var indice = el.dataset.indice;
    var id = el.dataset.idjogo;
    var situacao = el.dataset.situacao;
    var vencedorWO = el.dataset.vencedor;
    var desafiante_id = el.dataset.desafianteid;
    var desafiado_id = el.dataset.desafiadoid;
    var placar = el.dataset.placar;
    var jogoFaseGrupo = el.dataset.fasegrupo;
    var nomeDesafiante = "";
    var nomeDesafiado = "";
    var nomeDesafiante2 = "";
    var nomeDesafiado2 = "";
    if (origem == "TABELA") {
        nomeDesafiante = document.getElementById("desafiante" + id).value;
        nomeDesafiado = document.getElementById("desafiado" + id).value;
        nomeDesafiante2 = document.getElementById("desafiante2" + id).value;
        nomeDesafiado2 = document.getElementById("desafiado2" + id).value;
    }
    else {
        nomeDesafiante = ObterNomeJogadorSelecionado(indice, "desafiante");
        nomeDesafiado = ObterNomeJogadorSelecionado(indice, "desafiado");

        if (nomeDesafiante == null && nomeDesafiado == null) {
            nomeDesafiante = document.getElementById("jogador1Nome_" + indice).value;

            var lblNomeDuplaDesafiante = document.getElementById("jogador1DuplaNome_" + indice);
            if (lblNomeDuplaDesafiante != null) {
                nomeDesafiante2 = lblNomeDuplaDesafiante.value;
            }
            nomeDesafiado = document.getElementById("jogador2Nome_" + indice).value;

            var lblNomeDuplaDesafiado = document.getElementById("jogador2DuplaNome_" + indice);
            if (lblNomeDuplaDesafiado != null) {
                nomeDesafiado2 = lblNomeDuplaDesafiado.value;
            }
        }
    }
    var str = placar.split('|');
    var set1Desafiante = str[0];
    var set1Desafiado = str[1];
    var set2Desafiante = str[2];
    var set2Desafiado = str[3];
    var set3Desafiante = str[4];
    var set3Desafiado = str[5];
    $(".modal-body #Id").val(id);
    $(".modal-body #Origem").val(origem);
    $(".modal-body #JogoFaseGrupo").val(jogoFaseGrupo);
    $(".modal-body #qtddGames1setDesafiante").val(set1Desafiante);
    $(".modal-body #qtddGames1setDesafiado").val(set1Desafiado);
    $(".modal-body #qtddGames2setDesafiante").val(set2Desafiante);
    $(".modal-body #qtddGames2setDesafiado").val(set2Desafiado);
    $(".modal-body #qtddGames3setDesafiante").val(set3Desafiante);
    $(".modal-body #qtddGames3setDesafiado").val(set3Desafiado);
    $(".modal-body #vDesafiante").val(desafiante_id);
    $(".modal-body #vDesafiado").val(desafiado_id);
    $(".modal-body #desafiante_id").val(desafiante_id);
    $(".modal-body #desafiado_id").val(desafiado_id);
    if (vencedorWO == desafiante_id) {
        $(".modal-body #vDesafiante").attr('checked', true);
    } else {
        $(".modal-body #vDesafiado").attr('checked', true);
    }

    $("select#situacao_id").val(situacao);
    exibirOpcaoVencedores(situacao);
    if (nomeDesafiante2 != undefined && nomeDesafiante2 != "") {
        $(".modal-body #nomeDesafiante").html(nomeDesafiante + ' / ' + nomeDesafiante2);
    }
    else {
        $(".modal-body #nomeDesafiante").html(nomeDesafiante);
    }

    if (nomeDesafiado2 != undefined && nomeDesafiado2 != "") {
        $(".modal-body #nomeDesafiado").html(nomeDesafiado + ' / ' + nomeDesafiado2);
    }
    else {
        $(".modal-body #nomeDesafiado").html(nomeDesafiado);
    }
    $(".modal-body #vencedorDesafiadoNome").html(nomeDesafiado);
    $(".modal-body #vencedorDesafianteNome").html(nomeDesafiante);
}

$("#placarForm").submit(function (event) {
    event.preventDefault();

    ValidarConsolidacaoPontosFaseGrupo();

    return false;
});

function ShowLoader(mostrar) {
    if (LoaderSelecionado == TipoLoader.CONSOLIDACAO) {
        if (mostrar) {
            loaderPageConsolidacao.show();
        }
        else {
            loaderPageConsolidacao.hide();
        }
    }
    else if (LoaderSelecionado == TipoLoader.PADRAO) {
        if (mostrar) {
            loaderPage.show();
        }
        else {
            loaderPage.hide();
        }
    }
    else {
        if (mostrar) {
            loaderPage.show();
            loaderPageConsolidacao.show();
        }
        else {
            loaderPage.hide();
            loaderPageConsolidacao.hide();
        }
    }
}

function ValidarConsolidacaoPontosFaseGrupo() {
    var id = $(".modal-body #Id").val();
    $.ajax({
        type: "GET",
        url: "/Torneio/ValidarConsolidacaoPontosFaseGrupo",
        dataType: "json",
        data: {
            "jogoId": id
        },
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
                    if (response.status == "CONSOLIDAR") {
                        LoaderSelecionado = TipoLoader.CONSOLIDACAO;
                    }
                    else {
                        LoaderSelecionado = TipoLoader.PADRAO;
                    }

                    ShowLoader(true);

                    //LANÇAMENTO DO PLACAR - INICIO 
                    var url = "LancarResultado"
                    if (document.getElementById("situacao_id").value == '5') {
                        url = "LancarWO";
                    }
                    submitForm(url);
                    //LANÇAMENTO DO PLACAR - FIM 
                }
            }
        },
        error: function (jqXHR, textStatus, errorThrown) {
            toastr.error(errorThrown, "Erro");
        }
    });

}

function submitForm(url) {
    $.ajax({
        type: "POST",
        url: url,
        cache: false,
        dataType: "json",
        data: $('form#placarForm').serialize(),
        success: function (resp) {
            if (resp.retorno == 0) {
                ShowLoader(false);
                toastr.error(resp.erro, "Erro");
            } else {
                var id = $(".modal-body #Id").val();
                var situacaoId = document.getElementById("situacao_id").value;

                var qtddGames1setDesafiado = $(".modal-body #qtddGames1setDesafiado").val();
                var qtddGames1setDesafiante = $(".modal-body #qtddGames1setDesafiante").val();
                var qtddGames2setDesafiado = $(".modal-body #qtddGames2setDesafiado").val();
                var qtddGames2setDesafiante = $(".modal-body #qtddGames2setDesafiante").val();
                var qtddGames3setDesafiado = $(".modal-body #qtddGames3setDesafiado").val();
                var qtddGames3setDesafiante = $(".modal-body #qtddGames3setDesafiante").val();

                if ($(".modal-body #Origem").val() != "TABELA") {
                    if ((situacaoId == 1 || situacaoId == 2) && (qtddGames1setDesafiado != 0 || qtddGames1setDesafiante != 0)) {
                        $("#btnPlacar" + id).attr('data-situacao', 4);
                        $("#badge" + id).attr('class', 'badge bg-green');
                        $("#badge" + id).html('F');
                    } else {
                        $("#btnPlacar" + id).attr('data-situacao', situacaoId);
                    }
                    if (situacaoId == 1) {
                        $("#btnPlacar" + id).attr('data-placar', "0|0|0|0|0|0")
                    } else {
                        $("#btnPlacar" + id).attr('data-placar', qtddGames1setDesafiante + "|" + qtddGames1setDesafiado + "|" +
                            qtddGames2setDesafiante + "|" + qtddGames2setDesafiado + "|" +
                            qtddGames3setDesafiante + "|" + qtddGames3setDesafiado);
                    }
                    if (situacaoId == '5' || situacaoId == '6' || situacaoId == '4') {
                        $("#badge" + id).attr('class', 'badge bg-green');
                        $("#badge" + id).html('F');
                    } else if (situacaoId == '1' && (qtddGames1setDesafiante != '0' || qtddGames1setDesafiado != '0')) {
                        $("#badge" + id).attr('class', 'badge bg-green');
                        $("#badge" + id).html('F');
                    } else if (situacaoId == '1') {
                        $("#badge" + id).attr('class', 'badge bg-red');
                        $("#badge" + id).html('P');
                    }
                }
                else {

                    var placarDosJogos = qtddGames1setDesafiado + "/" + qtddGames1setDesafiante + " - " + qtddGames2setDesafiado + "/" + qtddGames2setDesafiante;
                    if (qtddGames3setDesafiado != 0 || qtddGames3setDesafiante != 0) {
                        placarDosJogos = placarDosJogos + " - " + qtddGames3setDesafiado + "/" + qtddGames3setDesafiante;
                    }

                    if ((situacaoId == 1 || situacaoId == 2) && (qtddGames1setDesafiado != 0 || qtddGames1setDesafiante != 0)) {
                        $("#btnPlacar" + id).attr('data-situacao', 4);
                    } else {
                        $("#btnPlacar" + id).attr('data-situacao', situacaoId);
                    }

                    if (situacaoId == 1) {
                        $("#btnPlacar" + id).attr('data-placar', "0|0|0|0|0|0")
                    } else {
                        $("#btnPlacar" + id).attr('data-placar', qtddGames1setDesafiante + "|" + qtddGames1setDesafiado + "|" +
                            qtddGames2setDesafiante + "|" + qtddGames2setDesafiado + "|" +
                            qtddGames3setDesafiante + "|" + qtddGames3setDesafiado);
                    }


                    if (situacaoId == '4' && $(".modal-body #desafiante_id").val() != 10) {
                        //Finalizado
                        document.getElementById("placarResultado" + id).innerText = placarDosJogos;
                        AtualizarBotaoLancarPlacar(id, true);
                    }
                    else if (situacaoId == '5' && $(".modal-body #desafiante_id").val() != 10) {
                        //WO
                        document.getElementById("placarResultado" + id).innerText = "WO";
                        AtualizarBotaoLancarPlacar(id, true);
                    }
                    else if (situacaoId == '6') {
                        //Desistencia
                        document.getElementById("placarResultado" + id).innerText = placarDosJogos + " desist.";
                        AtualizarBotaoLancarPlacar(id, true);
                    }
                    else if ((situacaoId == 1 || situacaoId == 2) && (qtddGames1setDesafiado != 0 || qtddGames1setDesafiante != 0)) {
                        //Finalizado
                        document.getElementById("placarResultado" + id).innerText = placarDosJogos;
                        AtualizarBotaoLancarPlacar(id, true);
                    }
                    else {
                        document.getElementById("placarResultado" + id).innerText = "";
                        AtualizarBotaoLancarPlacar(id, false);
                    }
                }
                ShowLoader(false);
                $("#placar-modal").modal('hide');
                toastr.options = {
                    "positionClass": "toast-top-center",
                    "timeOut": 10000
                }
                if (resp.pontuacaoLiga == 1) {
                    toastr.success("<a href='https://www.rankingdetenis.com/Ranking/RankingDasLigas'>Torneio Finalizado: A pontuação da liga foi gerada. CLIQUE AQUI para acessar a pontuação.</a>", "Aviso");
                } else {
                    toastr.success("Atualização realizada com sucesso.", "Aviso");
                }

                if (situacaoId == '5' && document.getElementById("classeEhFaseGrupo").value == "1" && $(".modal-body #JogoFaseGrupo").val() != "") {
                    $("#modalNotificaWO").modal('show');
                }
                else if ($(".modal-body #Origem").val() == "TABELA") {
                    AtualizarTabela()
                }

            }

        }
    });
}

function exibirOpcaoVencedores(situacao) {
    if (situacao == '5') {
        document.getElementById("IndicadorVencedor").classList.add("show");
    } else {
        if (situacao == '1') {
            $(".modal-body #qtddGames1setDesafiante").val(0);
            $(".modal-body #qtddGames1setDesafiado").val(0);
            $(".modal-body #qtddGames2setDesafiante").val(0);
            $(".modal-body #qtddGames2setDesafiado").val(0);
            $(".modal-body #qtddGames3setDesafiante").val(0);
            $(".modal-body #qtddGames3setDesafiado").val(0);
        }
        document.getElementById("IndicadorVencedor").classList.remove("show");
    }
}

function AtualizarBotaoLancarPlacar(id, ehEdicao) {
    if (ehEdicao) {
        $("#btnPlacar" + id).html("<span style='font-size: 9px;' class='glyphicon glyphicon-edit'></span>");
    }
    else {
        $("#btnPlacar" + id).html("<span style='font-size: 10px;' class='glyphicon glyphicon-plus'></span> Lançar placar");
    }
}

function AtualizarTabela() {
    document.location.reload(true);
}

document.getElementById("IndicadorVencedor").classList.remove("show");
