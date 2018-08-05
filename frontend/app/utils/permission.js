export const ProjCanStop = ({ status }) => {
  switch (status) {
    case 'running':
      return true;
    default:
      return false;
  }
};

export const ProjCanDrop = ({ status }) => {
  switch (status) {
    case 'error':
    case 'done':
      return true;
    default:
      return false;
  }
};

export const CatCanStop = ({ status }) => {
  switch (status) {
    case 'init':
      return true;
    case 'iter':
      return true;
    case 'running':
      return true;
    default:
      return false;
  }
};

export const EvalCanStop = ({ status }) => {
  switch (status) {
    case 'Grun':
      return true;
    case 'Mrun':
      return true;
    case 'Erun':
      return true;
    case 'Prun':
      return true;
    default:
      return false;
  }
};
