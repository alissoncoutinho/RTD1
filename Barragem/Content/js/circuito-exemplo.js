const swiper = new Swiper('.swiper', {
    // Optional parameters
    direction: 'horizontal',
    loop: false,
    slidesPerView: "auto",
    centeredSlides: false,
    spaceBetween: 10,
    freeMode: true,
        breakpoints: {
            992: {
              slidesPerView: 4,
              spaceBetween: 0,
            },    
          },
  });

  const swiper2 = new Swiper('.swiper2', {
    // Optional parameters
    direction: 'horizontal',
    loop: false,
    slidesPerView: "auto",
    centeredSlides: false,
    spaceBetween: 10,
    freeMode: true,
        breakpoints: {
            992: {
              slidesPerView: "auto",
              spaceBetween: 0,
            },    
          },
  });


  //validar qual opção está etivada
  let options = document.getElementById('options')
  let circuito1 = document.getElementById('circuito1');
  let circuito2 = document.getElementById('circuito2');
  let slideTabs1 = document.getElementById('slide-circuito1')
  let slideTabs2 = document.getElementById('slide-circuito2')

  options.addEventListener('change',()=>{
      if(circuito1.selected){
        slideTabs1.classList.remove('unactive');
        slideTabs2.classList.add('unactive');
      }else if(circuito2.selected){
        slideTabs1.classList.add('unactive');
        slideTabs2.classList.remove('unactive');
      }
   
  });
    


