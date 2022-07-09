var myApp;
myApp = myApp || (function () {
    var pleaseWaitDiv = $('#WaitDialog');
    return {
        showPleaseWait: function () {
            pleaseWaitDiv.modal();
        },
        hidePleaseWait: function () {
            pleaseWaitDiv.modal('hide');
        },

    };
})();

$(document).ready(function () {

    $.fn.datepicker.defaults.format = "dd/mm/yyyy";

    $('.alert .close').on("click", function (e) {
        $(this).parent().hide();
    });


    $(".confirmDialog").confirm({
        title: "Confirmar Operação",
        content: "Tem certeza que deseja realizar esta operação?",
        buttons: {
            sim: {
                text: 'Sim',
                btnClass: 'btn-primary',
                action: function () {
                    location.href = this.$target.data("link");
                }
            },
            cancelar: function () { }
        }
    });

    $(".confirmExclusaoRodada").confirm({
        title: "Confirmar Operação",
        content: "Tem certeza que deseja EXCLUIR PERMANENTEMENTE esta rodada, os seus respectivos jogos e o ranking caso exista?",
        buttons: {
            sim: {
                text: 'Sim',
                btnClass: 'btn-primary',
                action: function () {
                    ValidarExclusaoRodada(this.$target.data("id"), this.$target.data("link"));
                }
            },
            cancelar: function () { }
        }
    });

    function ValidarExclusaoRodada(id, link) {
        $.ajax({
            type: "GET",
            url: "/Rodada/ValidarExclusaoRodada",
            dataType: "json",
            data: {
                "id": id
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
                            $.alert({
                                title: '<a style=\'color:red\'> ATENÇÃO!!! </a>',
                                content: response.mensagem
                            });
                        }
                        else {
                            location.href = link;
                        }
                    }
                }
            },
            error: function (jqXHR, textStatus, errorThrown) {
                toastr.error(errorThrown, "Erro");
            }
        });
    }

});


