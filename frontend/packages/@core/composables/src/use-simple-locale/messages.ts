export type Locale = 'en-US' | 'ko-KR' | 'zh-TW';

export const messages: Record<Locale, Record<string, string>> = {
  'en-US': {
    cancel: 'Cancel',
    collapse: 'Collapse',
    confirm: 'Confirm',
    expand: 'Expand',
    prompt: 'Prompt',
    reset: 'Reset',
    submit: 'Submit',
  },
  'ko-KR': {
    cancel: '취소',
    collapse: '접기',
    confirm: '확인',
    expand: '펼치기',
    prompt: '알림',
    reset: '초기화',
    submit: '제출',
  },
  'zh-TW': {
    cancel: '取消',
    collapse: '收起',
    confirm: '確認',
    expand: '展開',
    prompt: '提示',
    reset: '重設',
    submit: '提交',
  },
};

export const getMessages = (locale: Locale) => messages[locale];
