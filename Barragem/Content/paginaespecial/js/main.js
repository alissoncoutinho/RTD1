function checarRegistrosBanner() {
    var qtdeRegistros = document.getElementById("totalRegistrosBanner").value;
    if (qtdeRegistros == 0) {
        document.getElementById("sectionDetalhesPrimeiroTorneioBanner").style.display = "none";
    }
}

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


let optionsBanner = {};
let totalRegistrosBanner = document.getElementById("totalRegistrosBanner").value;
if (totalRegistrosBanner == 1) {
    optionsBanner = {
        direction: 'horizontal',
        loop: false,
        slidesPerView: "auto",
        spaceBetween: 20,
        slideToClickedSlide: true,
        navigation: {
            nextEl: '.swiper-button-next',
            prevEl: '.swiper-button-prev',
        },
        thumbs: {
            swiper: gallery,
        },
    };
}
else {
    optionsBanner = {
        direction: 'horizontal',
        loop: true,
        slidesPerView: "auto",
        spaceBetween: 20,
        slideToClickedSlide: true,
        navigation: {
            nextEl: '.swiper-button-next',
            prevEl: '.swiper-button-prev',
        },
        thumbs: {
            swiper: gallery,
        },
    };
}
const swiper = new Swiper('.swiper', optionsBanner);

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

swiper.on('slideNextTransitionStart', obterDetalhesBannerSelecionado);
swiper.on('slidePrevTransitionStart', obterDetalhesBannerSelecionado);

function obterDetalhesBannerSelecionado () {
    let slides = [...swiper.$wrapperEl[0].children]
    let slideActive = slides.find(active => active.className == 'swiper-slide swiper-slide-active');
    let slideActiveRepater = slides.find(active => active.className == 'swiper-slide swiper-slide-duplicate-active');

    if (slideActive != undefined) {
        torneio.textContent = slideActive.children[1].innerText
        data.textContent = slideActive.children[2].innerText
        if (slideActive.children[4].value > 0) {
            localPontuacao.textContent = slideActive.children[3].value + ' - ' + slideActive.children[4].value + ' pontos'
        }
        else {
            localPontuacao.textContent = slideActive.children[3].value
        }
        if (slideActive.children[7].value == 'ABERTA') {
            inscrevaSe.style.display = "block";
            inscrevaSe.href = slideActive.children[5].value
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
        if (slideActiveRepater.children[4].value > 0) {
            localPontuacao.textContent = slideActiveRepater.children[3].value + ' - ' + slideActiveRepater.children[4].value + ' pontos'
        }
        else {
            localPontuacao.textContent = slideActiveRepater.children[3].value
        }
        if (slideActiveRepater.children[7].value == 'ABERTA') {
            inscrevaSe.style.display = "block";
            inscrevaSe.href = slideActiveRepater.children[5].value
        }
        else {
            inscrevaSe.style.display = "none";
        }
        imagemBanner.src = slideActiveRepater.children[0].src
        imagemBannerMobile.src = slideActiveRepater.children[8].value
    }
}

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

//alternativa para tablist
const slider = document.querySelectorAll('.slide');
let isDown = false;
let startX;
let scrollLeft;

slider.forEach(slide => {
    slide.addEventListener('mousedown', (e) => {
        isDown = true;
        slide.classList.add('active');
        startX = e.pageX - slide.offsetLeft;
        scrollLeft = slide.scrollLeft;
    });
    slide.addEventListener('mouseleave', () => {
        isDown = false;
        slide.classList.remove('active');
    });
    slide.addEventListener('mouseup', () => {
        isDown = false;
        slide.classList.remove('active');
    });
    slide.addEventListener('mousemove', (e) => {
        if (!isDown) return;
        e.preventDefault();
        const x = e.pageX - slide.offsetLeft;
        const walk = (x - startX) * 3; //scroll-fast
        slide.scrollLeft = scrollLeft - walk;
    });
});

checarRegistrosBanner();
obterDetalhesBannerSelecionado();