let copyLink = document.getElementById('copy-link');
var copyLinkUrl = copyLink.getAttribute('data-link');

function copyTextToClipboard(text) {
    if (!navigator.clipboard) {
      fallbackCopyTextToClipboard(text);
      return;
    }
    navigator.clipboard.writeText(text).then(function() {
      console.log('Async: Copying to clipboard was successful!');
    }, function(err) {
      console.error('Async: Could not copy text: ', err);
    });
  }

copyLink.addEventListener('click', function(event) {
copyTextToClipboard(copyLinkUrl);
const Toast = Swal.mixin({
    toast: true,
    position: 'top-end',
    showConfirmButton: false,
    timer: 3000,
    timerProgressBar: true,
    didOpen: (toast) => {
        toast.addEventListener('mouseenter', Swal.stopTimer)
        toast.addEventListener('mouseleave', Swal.resumeTimer)
    }
    })
      
    Toast.fire({
    icon: 'success',
    title: 'Link copiado com sucesso'
    })
});

copyTextToClipboard(copyLinkUrl);

  