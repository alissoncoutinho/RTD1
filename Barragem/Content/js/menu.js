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