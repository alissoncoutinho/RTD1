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
                    location.href = this.$target.data("link");
                }
            },
            cancelar: function () { }
        }
    });
    
});


