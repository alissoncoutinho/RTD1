const swiper = new Swiper('.swiper', {
    // Optional parameters
    direction: 'horizontal',
    loop: false,
    centeredSlides: true,

  
    // Navigation arrows
    navigation: {
      nextEl: '.swiper-button-next',
      prevEl: '.swiper-button-prev',
    },
  
   
  });

  const swiper2 = new Swiper('.swiper2', {
    // Optional parameters
    direction: 'horizontal',
    loop: false,
    slidesPerView: "1",
    grabCursor: true,
    spaceBetween: 30,
    breakpoints: {
      768: {
        slidesPerView: 2,
        spaceBetween: 30,
      },
    },
  
    // If we need pagination
    pagination: {
      el: '.swiper-pagination',
      clickable: true,
    },  
  });

  const swiper3 = new Swiper('.swiper3', {
    // Optional parameters
    direction: 'horizontal',
    loop: false,
    slidesPerView: 2,
    grabCursor: true,
    spaceBetween: 30,
    freeMode: true,
    breakpoints: {
      768: {
        slidesPerView: 3,
        spaceBetween: 30, 
      },
    },
  
  });


  //scripts para o menu hamburger
  let burger = document.querySelector('.hamburger');
  let menu = document.querySelector('.menu');
  let links = document.querySelectorAll('a');
  burger.addEventListener('click',()=>{
    burger.classList.toggle('is-active');
    menu.classList.toggle('menu-active');
    if(menu.classList.contains('menu-active')){
      document.body.style.overflow = 'hidden';
    }else{
      document.body.style.overflow = 'auto';
    }
  });

  for(var i=0; i < links.length; i++){
    links[i].addEventListener('click',()=>{
      burger.classList.remove('is-active');
      menu.classList.remove('menu-active');
      document.body.style.overflow = 'auto';
    })
  }

