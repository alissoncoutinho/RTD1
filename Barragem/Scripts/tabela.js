jQuery(function() {

	jQuery('.mover_direita').click(function(){
		jQuery('#conteudo_tabela_jogos').scrollLeft( 300 );
	});
	jQuery('.mover_esquerda').click(function(){
		jQuery('#conteudo_tabela_jogos').scrollLeft( 0 );
	});

	$( window ).scroll(function() {
		if((jQuery(window).width() > "1198")){
		    nScrollPosition = $( window ).scrollTop();
		    if(nScrollPosition>=3300){
		         $( ".mover_esquerda, .mover_direita" ).css( "display", "none" );
		    }else{
		         $( ".mover_esquerda, .mover_direita" ).css( "display", "block" );
		    }
		}
	});
	

	//rodadas
	var rodadas = $('.box_rodada');
	var qtd_rodadas = rodadas.length;
	rodadas.each(function(i, e){
		var a;
		//jogos
		jQuery(this).find('.alinhamento').each(function(index, element){

			var ultima_partida = false;
			//var penultima_partida = false;
			var offset = jQuery(element).offset();

			if ((i === (qtd_rodadas - 1)) && (index == 0)) {
			    //ultima_partida = true;
			    ultima_partida = false;
			}

			var ajuste_mobile = 0;

			if((jQuery(window).width() < "770")){
				ajuste_mobile = -30;
			}

			// if ((i === (qtd_rodadas - 2)) && (index == 0)) {
			// 	penultima_partida = true;
			// }

			// var linhas_ajuste = "<div class='linhas_ajuste' style='top: "+(offset.top-299)+"px;'></div>";
			// var linhas = "<div class='linhas' style='top: "+(offset.top-295)+"px;'></div>";

			//primeira rodada
			if(i == 0){
				if((((index % 2) != 0) && (index != 0))){
					jQuery(element).addClass("primeira_rodada");
					jQuery(element).before("<div class='linhas_ajuste linhas_ajuste_1' style='top: "+(offset.top-346+ajuste_mobile)+"px;'></div>");
					jQuery(element).before("<div class='linhas linhas_1' style='top: "+(offset.top-342+ajuste_mobile)+"px;'></div>");
				}
			}

			//segunda rodada
			if(i == 1){
				jQuery(e).css('padding','52px 0px 0px');
				jQuery(element).addClass('segunda_rodada');
				if((((index % 2) != 0) && (index != 0))){
					jQuery(element).before("<div class='linhas_ajuste linhas_ajuste_2' style='top: "+(offset.top-439+ajuste_mobile)+"px;'></div>");
					jQuery(element).before("<div class='linhas linhas_2' style='top: "+(offset.top-435+ajuste_mobile)+"px;'></div>");
				}
			}

			//terceira rodada
			if(i == 2){
				jQuery(e).css('padding','160px 0px 0px');
				jQuery(element).addClass('terceira_rodada');
				if((((index % 2) != 0) && (index != 0))){
					jQuery(element).before("<div class='linhas_ajuste linhas_ajuste_3' style='top: "+(offset.top-649+ajuste_mobile)+"px;'></div>");
					jQuery(element).before("<div class='linhas linhas_3' style='top: "+(offset.top-645+ajuste_mobile)+"px;'></div>");
				}
			}

			//quarta rodada
			if(i == 3){
				jQuery(e).css('padding','372px 0px 0px');
				jQuery(element).addClass('quarta_rodada');
				if((((index % 2) != 0) && (index != 0))){
					jQuery(element).before("<div class='linhas_ajuste linhas_ajuste_4' style='top: "+(offset.top-1064+ajuste_mobile)+"px;'></div>");
					jQuery(element).before("<div class='linhas linhas_4' style='top: "+(offset.top-1060+ajuste_mobile)+"px;'></div>");
				}
			}

			//quinta rodada
			if(i == 4){
				jQuery(e).css('padding','785px 0px 0px');
				jQuery(element).addClass('quinta_rodada');
				if((((index % 2) != 0) && (index != 0))){
					jQuery(element).before("<div class='linhas_ajuste linhas_ajuste_5' style='top: "+(offset.top-1898+ajuste_mobile)+"px;'></div>");
					jQuery(element).before("<div class='linhas linhas_5' style='top: "+(offset.top-1894+ajuste_mobile)+"px;'></div>");
				}
			}

			//sexta rodada
			if(i == 5){
				
			}

			// if((i == 0) || (i == 1) || (i == 2) || (i == 3) || (i == 4)){
			// 	if((((index % 2) != 0) && (index != 0))){
			// 		jQuery(element).before(linhas_ajuste);
			// 		jQuery(element).before(linhas);
			// 	}
			// }

			// //quinta rodada
			// if((i == 4) && (index == 0)){
			// 	jQuery(e).css('padding','2292px 0px 0px 0px');
			// }

			// //sexta rodada
			// if((i == 5) && (index == 0)){
			// 	jQuery(e).css('padding','2352px 0px 0px 0px');
			// }

			var altura_pagina = jQuery('.display_jogos').outerHeight();
			//alert(altura_pagina);

			//alinha as divs caso seja penúltima partida
			// if(penultima_partida == true){
			// 	jQuery(e).css('padding', [((altura_pagina/2)-125)+'px 0px 0px 0px']);
			// }

			//alinha as divs caso seja última partida
			if(ultima_partida == true){
				jQuery(e).css('padding', [((altura_pagina/2)-64)+'px 0px 0px 0px']);
			}



		});
		//fim jogos

	});
	//fim rodadas

});