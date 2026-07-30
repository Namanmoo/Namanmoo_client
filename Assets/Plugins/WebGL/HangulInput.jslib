// 캔버스 위에 진짜 <input> 을 겹쳐 브라우저와 IME 가 글자를 만들게 한다.
//
// 왜 필요한가: 유니티 WebGL 은 캔버스에서 keydown 을 받아 키 코드를 읽는다. 그런데
// 한글은 키 코드로 오지 않는다 — 조합 중에는 keyCode 229("처리 중")만 오고, 완성된
// 글자는 compositionend/input 이벤트로만 나오며 그 이벤트는 편집 가능한 DOM 요소에만
//간다. <canvas> 는 편집 요소가 아니라 조합 대상이 될 수 없다. 일본어·중국어도 같다.
//
// 자리는 0~1 정규화 좌표로 받는다. 픽셀로 받으면 devicePixelRatio 와 템플릿의 16:9
// 레터박싱 때문에 어긋난다. 정규화 값에 캔버스의 실제 CSS 크기를 곱하면 둘 다 맞는다.
//
// 이 파일은 우리가 쓴 것이고, .jslib 은 유니티가 원래 제공하는 통로다 —
// 외부 패키지를 받아 넣는 것이 아니다.

mergeInto(LibraryManager.library, {

  $NaManMooInput: {
    element: null,
    owner: null,
    // 캔버스 크기가 바뀌면(창 조절·전체화면) 자리를 다시 맞춰야 한다
    place: null,
    onResize: null,

    canvas: function () {
      return document.querySelector('#unity-canvas') || document.querySelector('canvas');
    },

    reposition: function () {
      var self = NaManMooInput;
      if (!self.element || !self.place) return;

      var canvas = self.canvas();
      if (!canvas) return;

      var box = canvas.getBoundingClientRect();
      var p = self.place;

      // 유니티는 왼쪽 아래가 원점, CSS 는 왼쪽 위가 원점이라 y 를 뒤집는다
      var left = box.left + p.x * box.width;
      var top = box.top + (1 - p.y - p.h) * box.height;
      var width = p.w * box.width;
      var height = p.h * box.height;

      var style = self.element.style;
      style.left = left + 'px';
      style.top = top + 'px';
      style.width = width + 'px';
      style.height = height + 'px';
      // 글자 크기도 칸 높이를 따라가야 창을 줄였을 때 넘치지 않는다
      style.fontSize = Math.max(10, Math.floor(height * 0.62)) + 'px';
    },

    close: function (commit) {
      var self = NaManMooInput;
      if (!self.element) return;

      var value = self.element.value;
      var owner = self.owner;

      if (self.onResize) {
        window.removeEventListener('resize', self.onResize);
        self.onResize = null;
      }

      self.element.remove();
      self.element = null;
      self.owner = null;
      self.place = null;

      if (owner) {
        if (commit) {
          SendMessage(owner, 'OnWebTextChanged', value);
        }
        SendMessage(owner, 'OnWebTextClosed', commit ? '1' : '0');
      }

      // 포커스를 캔버스로 돌려주지 않으면 게임이 키를 못 받는다
      var canvas = self.canvas();
      if (canvas) canvas.focus();
    },
  },

  // 칸을 연다. x/y/w/h 는 0~1 정규화 좌표(유니티 기준, 왼쪽 아래 원점).
  //
  // __deps 를 빼면 이맥스크립튼이 $NaManMooInput 을 안 쓰는 것으로 보고 지워 버려
  // 빌드는 되는데 실행 중에 undefined 로 죽는다.
  NaManMooOpenText__deps: ['$NaManMooInput'],
  NaManMooOpenText: function (ownerPtr, valuePtr, placeholderPtr, x, y, w, h, maxLength) {
    var self = NaManMooInput;
    self.close(false);

    var canvas = self.canvas();
    if (!canvas) return;

    var input = document.createElement('input');
    input.type = 'text';
    input.value = UTF8ToString(valuePtr);
    input.placeholder = UTF8ToString(placeholderPtr);
    if (maxLength > 0) input.maxLength = maxLength;
    input.setAttribute('autocomplete', 'off');
    input.setAttribute('autocorrect', 'off');
    input.setAttribute('spellcheck', 'false');

    var style = input.style;
    style.position = 'fixed';
    style.zIndex = '20';
    style.margin = '0';
    style.padding = '0 8px';
    style.border = 'none';
    style.outline = 'none';
    style.boxSizing = 'border-box';
    // 유니티 쪽 칸과 같은 생김새로 — 그래야 갑자기 다른 게 뜬 것처럼 보이지 않는다
    style.background = 'rgba(255, 255, 255, 0.96)';
    style.color = '#262626';
    style.fontFamily = 'inherit';

    self.element = input;
    self.owner = UTF8ToString(ownerPtr);
    self.place = { x: x, y: y, w: w, h: h };

    document.body.appendChild(input);
    self.reposition();

    self.onResize = function () { self.reposition(); };
    window.addEventListener('resize', self.onResize);

    input.addEventListener('input', function () {
      if (self.owner) SendMessage(self.owner, 'OnWebTextChanged', input.value);
    });

    input.addEventListener('keydown', function (event) {
      // 조합 중(229)에는 손대지 않는다. 여기서 Enter 를 가로채면 한글 확정이 깨진다.
      if (event.isComposing || event.keyCode === 229) return;

      if (event.key === 'Enter') {
        event.preventDefault();
        self.close(true);
      } else if (event.key === 'Escape') {
        event.preventDefault();
        self.close(false);
      }
    });

    input.addEventListener('blur', function () { self.close(true); });

    input.focus();
    // 커서를 끝에 둔다 — 다시 열었을 때 앞에서 시작하면 이어 쓰기가 번거롭다
    var end = input.value.length;
    try { input.setSelectionRange(end, end); } catch (ignored) {}
  },

  NaManMooCloseText__deps: ['$NaManMooInput'],
  NaManMooCloseText: function () {
    NaManMooInput.close(true);
  },

  NaManMooIsTextOpen__deps: ['$NaManMooInput'],
  NaManMooIsTextOpen: function () {
    return NaManMooInput.element ? 1 : 0;
  },
});
