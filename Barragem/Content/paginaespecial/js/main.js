
//funções responsaveis por mudar o background do calendário

let kidTab = document.querySelectorAll('[data-kid-tab]')
let kidsImage = document.querySelector('[data-kid]')
kidTab.forEach(tab => {
    tab.addEventListener('click', () => {
        switch (tab.innerText) {
            case 'Tênis':
                kidsImage.src = '/Content/paginaespecial/images/calendario.png'
                break;
            case 'Kids':
                kidsImage.src = '/Content/paginaespecial/images/kid.png'
                break;
            case 'Beach Tennis':
                kidsImage.src = '/Content/paginaespecial/images/calendario-beach.png'
                break;
            default:
                break;
        }
    })
})

let tabelasTab = document.querySelectorAll('[data-tabelas]')
let tabelasImage = document.querySelector('[data-ranking]')
tabelasTab.forEach(tab => {
    tab.addEventListener('click', () => {
        switch (tab.innerText) {
            case 'Tênis':
                tabelasImage.src = '/Content/paginaespecial/images/ranking-tenis.jpg'
                break;
            case 'Beach Tennis':
                tabelasImage.src = '/Content/paginaespecial/images/ranking-beach.jpg'
                break;
            case 'Kids':
                tabelasImage.src = '/Content/paginaespecial/images/ranking-kids.jpg'
                break;
            default:
                break;
        }
    })
})

const gallery = new Swiper(".mySwiper2", {
    spaceBetween: 10,
    watchSlidesProgress: true,
    effect: "fade",
    fadeEffect: {
        crossFade: true
    },
    loop: false,
});

const swiper = new Swiper('.swiper', {
    // Optional parameters
    direction: 'horizontal',
    loop: true,
    slidesPerView: "auto",
    spaceBetween: 20,
    slideToClickedSlide: true,
    // loopAdditionalSlides:4,
    // autoplay:true,
    // loopedSlides:4,
    // initialSlide:0,
    // centeredSlides:true,
    navigation: {
        nextEl: '.swiper-button-next',
        prevEl: '.swiper-button-prev',
    },
    thumbs: {
        swiper: gallery,
    },
    // on:{
    //   click: function(e){
    //     e.slideNext(200, true)
    //   }
    // }
});

swiper.on('init', () => {
    console.log('iniciado')
})


//funções responsaveis por mudar o background do topo
let torneio = document.querySelector('[data-torneio]')
let data = document.querySelector('[data-date]')
let bgImage = document.querySelector('[data-background]')
let localPontuacao = document.querySelector('[data-localPontuacao]')
let inscrevaSe = document.querySelector('[data-inscrevase]')
let imagemBanner = document.querySelector('[data-imagem-banner]')
let imagemBannerMobile = document.querySelector('[data-imagem-banner-mobile]')

swiper.on('slideNextTransitionStart', function (e) {
    let slides = [...swiper.$wrapperEl[0].children]
    let slideActive = slides.find(active => active.className == 'swiper-slide swiper-slide-active');
    let slideActiveRepater = slides.find(active => active.className == 'swiper-slide swiper-slide-duplicate-active');

    if (slideActive != undefined) {
        torneio.textContent = slideActive.children[1].innerText
        data.textContent = slideActive.children[2].innerText
        localPontuacao.textContent = slideActive.children[3].value + ' - ' + slideActive.children[4].value + ' pontos'
        if (slideActive.children[7].value == 'ABERTA') {
            inscrevaSe.style.display = "block";
            inscrevaSe.href = slideActive.children[5].value
            inscrevaSe.textContent = 'Inscreva-se até ' + slideActive.children[6].value
        }
        else {
            inscrevaSe.style.display = "none";
        }
        imagemBanner.src = slideActive.children[0].src
        imagemBannerMobile.src = slideActive.children[8].value
    }

    if (slideActiveRepater != undefined) {
        torneio.textContent = slideActiveRepater.children[1].innerText
        data.textContent = slideActiveRepater.children[2].innerText
        localPontuacao.textContent = slideActiveRepater.children[3].value + ' - ' + slideActiveRepater.children[4].value + ' pontos'
        if (slideActiveRepater.children[7].value == 'ABERTA') {
            inscrevaSe.style.display = "block";
            inscrevaSe.href = slideActiveRepater.children[5].value
            inscrevaSe.textContent = 'Inscreva-se até ' + slideActiveRepater.children[6].value
        }
        else {
            inscrevaSe.style.display = "none";
        }
        imagemBanner.src = slideActiveRepater.children[0].src
        imagemBannerMobile.src = slideActiveRepater.children[8].value
    }

});

swiper.on('slidePrevTransitionStart', function () {
    let slides = [...swiper.$wrapperEl[0].children]
    let slideActive = slides.find(active => active.className == 'swiper-slide swiper-slide-active');
    let slideActiveRepater = slides.find(active => active.className == 'swiper-slide swiper-slide-duplicate-active');

    if (slideActive != undefined) {
        torneio.textContent = slideActive.children[1].innerText
        data.textContent = slideActive.children[2].innerText
        localPontuacao.textContent = slideActive.children[3].value + ' - ' + slideActive.children[4].value + ' pontos'
        if (slideActive.children[7].value == 'ABERTA') {
            inscrevaSe.style.display = "block";
            inscrevaSe.href = slideActive.children[5].value
            inscrevaSe.textContent = 'Inscreva-se até ' + slideActive.children[6].value
        }
        else {
            inscrevaSe.style.display = "none";
        }
        imagemBanner.src = slideActive.children[0].src
        imagemBannerMobile.src = slideActive.children[8].value
    }

    if (slideActiveRepater != undefined) {
        torneio.textContent = slideActiveRepater.children[1].innerText
        data.textContent = slideActiveRepater.children[2].innerText
        localPontuacao.textContent = slideActiveRepater.children[3].value + ' - ' + slideActiveRepater.children[4].value + ' pontos'
        if (slideActiveRepater.children[7].value == 'ABERTA') {
            inscrevaSe.style.display = "block";
            inscrevaSe.href = slideActiveRepater.children[5].value
            inscrevaSe.textContent = 'Inscreva-se até ' + slideActiveRepater.children[6].value
        }
        else {
            inscrevaSe.style.display = "none";
        }
        imagemBanner.src = slideActiveRepater.children[0].src
        imagemBannerMobile.src = slideActiveRepater.children[8].value
    }
})


//swipper Patrocinadores
const swiper2 = new Swiper('.mySwiper', {
    // Optional parameters
    direction: 'horizontal',
    loop: false,
    slidesPerView: 2,
    spaceBetween: 20,
    centerInsufficientSlides: true,
    centeredSlides: false,
    slideToClickedSlide: false,
    breakpoints: {
        640: {
            slidesPerView: 2,
            spaceBetween: 20,
        },
        768: {
            slidesPerView: 3,
            spaceBetween: 20,
        },
        1024: {
            slidesPerView: 6,
            spaceBetween: 20,
        },
    },
    // Navigation arrows
    navigation: {
        nextEl: '.swiper-button-next',
        prevEl: '.swiper-button-prev',

    },

    pagination: {
        el: ".swiper-pagination",
        clickable: true,
    },

});

const swiper4 = new Swiper('.swiper3', {
    // Optional parameters
    direction: 'horizontal',
    loop: false,
    slidesPerView: 'auto',
    spaceBetween: 0,
    freeMode: true,
    preventInteractionOnTransition: true,
    centeredSlides: false,
    slideToClickedSlide: true,
    setWrapperSize: true,
    breakpoints: {
        640: {
            slidesPerView: 'auto',
            spaceBetween: 0,
        },
        768: {
            slidesPerView: 'auto',
            spaceBetween: 0,
        },
        1200: {
            slidesPerView: 'auto',
            spaceBetween: 0,
        },
    },

});
