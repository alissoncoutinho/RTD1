let todos = document.querySelector('.todos');
let regras = document.querySelector('.federacao');
let input = document.getElementById('options');


input.addEventListener('change', ()=>{
    console.log(input.value);
    if(input.value !=  'regras'){
        todos.style.display = 'block';
        regras.style.display = 'none';
        
    }else{
     
        regras.style.display = 'block';
        todos.style.display = 'none';
    }
    
});