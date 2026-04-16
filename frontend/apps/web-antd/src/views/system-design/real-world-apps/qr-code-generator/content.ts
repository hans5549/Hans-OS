export type ApiMethod = 'DELETE' | 'GET' | 'POST' | 'PUT';
export type FlowKey = 'create' | 'retrieve';

export const sectionIds = [
  'why-hard',
  'requirements',
  'api-surface',
  'lifecycle',
  'scaling',
  'trade-offs',
] as const;

export type SectionId = (typeof sectionIds)[number];

type Translate = (key: string, args?: Array<number | string>) => string;

export interface ApiCard {
  description: string;
  method: ApiMethod;
  path: string;
  request: string[];
  response: string[];
  snippet: string;
  title: string;
}

export interface DesignTarget {
  label: string;
  note: string;
  value: string;
}

export interface GlossaryTerm {
  description: string;
  id: string;
  label: string;
}

export interface SectionMeta {
  eyebrow: string;
  id: SectionId;
  takeaway: string;
  title: string;
}

interface HeroHighlight {
  description: string;
  title: string;
}

interface DiagramItem {
  label: string;
  title: string;
}

interface ArtifactChip {
  label: string;
  value: string;
}

interface BadgeContent {
  basedOnPdf: string;
  caseStudy: string;
  frontendOnly: string;
  readingTime: string;
}

interface HeroVisual {
  chips: ArtifactChip[];
  description: string;
  kicker: string;
  nodes: DiagramItem[];
  title: string;
}

interface HeroContent {
  description: string;
  highlights: HeroHighlight[];
  kicker: string;
  learnKicker: string;
  targetsNote: string;
  targetsTitle: string;
  title: string;
  visual: HeroVisual;
}

interface RequirementGroup {
  items: string[];
  title: string;
}

interface FlowStep {
  description: string;
  title: string;
}

interface ScalingCard {
  bullets: string[];
  title: string;
}

interface TensionCard {
  description: string;
  title: string;
}

interface TradeOffCard {
  decision: string;
  reason: string;
}

interface FooterContent {
  relatedCases: string[];
  relatedComingSoon: string;
  relatedKicker: string;
  relatedTitle: string;
  sourceNote: string;
  takeaways: string[];
  takeawaysKicker: string;
  takeawaysTitle: string;
}

interface GlossaryContent {
  collapsePattern: string;
  expandPattern: string;
  kicker: string;
  terms: GlossaryTerm[];
  title: string;
}

interface NavigationContent {
  desktopTocAriaLabel: string;
  inPageLabel: string;
  mobileTocAriaLabel: string;
  progressLabel: string;
  startReading: string;
}

interface SidebarContent {
  currentSectionLabel: string;
  difficultyLabel: string;
  difficultyValue: string;
  scopeLabel: string;
  scopeValue: string;
  tocKicker: string;
  tocTitle: string;
}

interface UiContent {
  copyApiAriaPattern: string;
  copyButton: string;
  copyFailure: string;
  copySuccess: string;
  copyUnsupported: string;
  requestLabel: string;
  responseLabel: string;
}

interface SectionBundle {
  apiSurfaceSection: SectionMeta;
  lifecycleSection: SectionMeta;
  requirementsSection: SectionMeta;
  scalingSection: SectionMeta;
  sections: SectionMeta[];
  tradeOffsSection: SectionMeta;
  whyHardSection: SectionMeta;
}

export interface QrCodeGeneratorPageContent {
  apiCards: ApiCard[];
  apiSurfaceSection: SectionMeta;
  badges: BadgeContent;
  designTargets: DesignTarget[];
  footer: FooterContent;
  flowOptions: Array<{ label: string; value: FlowKey }>;
  flowSteps: Record<FlowKey, FlowStep[]>;
  glossary: GlossaryContent;
  hero: HeroContent;
  lifecycleSection: SectionMeta;
  navigation: NavigationContent;
  requirementGroups: RequirementGroup[];
  requirementsSection: SectionMeta;
  scalingCards: ScalingCard[];
  scalingSection: SectionMeta;
  sections: SectionMeta[];
  sidebar: SidebarContent;
  tensionCards: TensionCard[];
  tradeOffs: TradeOffCard[];
  tradeOffsSection: SectionMeta;
  ui: UiContent & {
    getCopyApiAriaLabel: (label: string) => string;
    getGlossaryToggleAriaLabel: (label: string, expanded: boolean) => string;
  };
  whyHardSection: SectionMeta;
}

const baseKey = 'page.systemDesign.qrCodeGeneratorPage';
const key = (path: string) => `${baseKey}.${path}`;

function interpolateLabel(pattern: string, label: string) {
  return pattern.replace('{label}', label);
}

function createSectionBundle(t: Translate): SectionBundle {
  const whyHardSection: SectionMeta = {
    eyebrow: t(key('sections.whyHard.eyebrow')),
    id: 'why-hard',
    takeaway: t(key('sections.whyHard.takeaway')),
    title: t(key('sections.whyHard.title')),
  };

  const requirementsSection: SectionMeta = {
    eyebrow: t(key('sections.requirements.eyebrow')),
    id: 'requirements',
    takeaway: t(key('sections.requirements.takeaway')),
    title: t(key('sections.requirements.title')),
  };

  const apiSurfaceSection: SectionMeta = {
    eyebrow: t(key('sections.apiSurface.eyebrow')),
    id: 'api-surface',
    takeaway: t(key('sections.apiSurface.takeaway')),
    title: t(key('sections.apiSurface.title')),
  };

  const lifecycleSection: SectionMeta = {
    eyebrow: t(key('sections.lifecycle.eyebrow')),
    id: 'lifecycle',
    takeaway: t(key('sections.lifecycle.takeaway')),
    title: t(key('sections.lifecycle.title')),
  };

  const scalingSection: SectionMeta = {
    eyebrow: t(key('sections.scaling.eyebrow')),
    id: 'scaling',
    takeaway: t(key('sections.scaling.takeaway')),
    title: t(key('sections.scaling.title')),
  };

  const tradeOffsSection: SectionMeta = {
    eyebrow: t(key('sections.tradeOffs.eyebrow')),
    id: 'trade-offs',
    takeaway: t(key('sections.tradeOffs.takeaway')),
    title: t(key('sections.tradeOffs.title')),
  };

  const sections = [
    whyHardSection,
    requirementsSection,
    apiSurfaceSection,
    lifecycleSection,
    scalingSection,
    tradeOffsSection,
  ];

  return {
    apiSurfaceSection,
    lifecycleSection,
    requirementsSection,
    scalingSection,
    sections,
    tradeOffsSection,
    whyHardSection,
  };
}

function createBadges(t: Translate): BadgeContent {
  return {
    basedOnPdf: t(key('badges.basedOnPdf')),
    caseStudy: t(key('badges.caseStudy')),
    frontendOnly: t(key('badges.frontendOnly')),
    readingTime: t(key('badges.readingTime')),
  };
}

function createGlossary(t: Translate): GlossaryContent {
  return {
    collapsePattern: t(key('glossary.collapsePattern')),
    expandPattern: t(key('glossary.expandPattern')),
    kicker: t(key('glossary.kicker')),
    terms: [
      {
        description: t(key('glossary.terms.base62.description')),
        id: 'base62',
        label: t(key('glossary.terms.base62.label')),
      },
      {
        description: t(key('glossary.terms.collision.description')),
        id: 'collision',
        label: t(key('glossary.terms.collision.label')),
      },
      {
        description: t(key('glossary.terms.readHeavy.description')),
        id: 'read-heavy',
        label: t(key('glossary.terms.readHeavy.label')),
      },
      {
        description: t(key('glossary.terms.cdn.description')),
        id: 'cdn',
        label: t(key('glossary.terms.cdn.label')),
      },
    ],
    title: t(key('glossary.title')),
  };
}

function createUi(
  t: Translate,
  glossary: GlossaryContent,
): QrCodeGeneratorPageContent['ui'] {
  return {
    copyApiAriaPattern: t(key('ui.copyApiAriaPattern')),
    copyButton: t(key('ui.copyButton')),
    copyFailure: t(key('ui.copyFailure')),
    copySuccess: t(key('ui.copySuccess')),
    copyUnsupported: t(key('ui.copyUnsupported')),
    getCopyApiAriaLabel: (label: string) =>
      interpolateLabel(t(key('ui.copyApiAriaPattern')), label),
    getGlossaryToggleAriaLabel: (label: string, expanded: boolean) =>
      interpolateLabel(
        expanded ? glossary.collapsePattern : glossary.expandPattern,
        label,
      ),
    requestLabel: t(key('ui.requestLabel')),
    responseLabel: t(key('ui.responseLabel')),
  };
}

function createApiCards(t: Translate, responseLabel: string): ApiCard[] {
  return [
      {
        description: t(key('api.create.description')),
        method: 'POST',
        path: '/v1/qr_code',
        request: ['url'],
        response: ['code', 'data.qr_token'],
        snippet: `POST /v1/qr_code
{
  "url": "https://example.com"
}

${responseLabel}
{
  "code": 0,
  "data": {
    "qr_token": "Ab9x2Q"
  }
}`,
        title: t(key('api.create.title')),
      },
      {
        description: t(key('api.image.description')),
        method: 'GET',
        path: '/v1/qr_code_image/:qr_token',
        request: ['dimension', 'color', 'border'],
        response: ['code', 'data.image_location'],
        snippet: `GET /v1/qr_code_image/:qr_token?dimension=300&color=000000&border=10

${responseLabel}
{
  "code": 0,
  "data": {
    "image_location": "https://cdn.example.com/qrs/Ab9x2Q.png"
  }
}`,
        title: t(key('api.image.title')),
      },
      {
        description: t(key('api.edit.description')),
        method: 'PUT',
        path: '/v1/qr_code/:qr_token',
        request: ['url'],
        response: ['code', 'data'],
        snippet: `PUT /v1/qr_code/:qr_token
{
  "url": "https://example.com/new-target"
}

${responseLabel}
{
  "code": 0,
  "data": null
}`,
        title: t(key('api.edit.title')),
      },
      {
        description: t(key('api.delete.description')),
        method: 'DELETE',
        path: '/v1/qr_code/:qr_token',
        request: ['-'],
        response: ['code', 'data'],
        snippet: `DELETE /v1/qr_code/:qr_token

${responseLabel}
{
  "code": 0,
  "data": null
}`,
        title: t(key('api.delete.title')),
      },
      {
        description: t(key('api.lookup.description')),
        method: 'GET',
        path: '/v1/qr_code/:qr_token',
        request: ['-'],
        response: ['code', 'data.url'],
        snippet: `GET /v1/qr_code/:qr_token

${responseLabel}
{
  "code": 0,
  "data": {
    "url": "https://example.com/original"
  }
}`,
        title: t(key('api.lookup.title')),
      },
  ];
}

function createDesignTargets(t: Translate): DesignTarget[] {
  return [
      {
        label: t(key('hero.targets.availability.label')),
        note: t(key('hero.targets.availability.note')),
        value: t(key('hero.targets.availability.value')),
      },
      {
        label: t(key('hero.targets.redirectLatency.label')),
        note: t(key('hero.targets.redirectLatency.note')),
        value: t(key('hero.targets.redirectLatency.value')),
      },
      {
        label: t(key('hero.targets.qrCodes.label')),
        note: t(key('hero.targets.qrCodes.note')),
        value: t(key('hero.targets.qrCodes.value')),
      },
      {
        label: t(key('hero.targets.users.label')),
        note: t(key('hero.targets.users.note')),
        value: t(key('hero.targets.users.value')),
      },
    ];
}

function createFlowOptions(t: Translate): Array<{ label: string; value: FlowKey }> {
  return [
    { label: t(key('flow.create.label')), value: 'create' },
    { label: t(key('flow.retrieve.label')), value: 'retrieve' },
  ];
}

function createFlowSteps(t: Translate): Record<FlowKey, FlowStep[]> {
  return {
      create: [
        {
          description: t(key('flow.create.steps.receive.description')),
          title: t(key('flow.create.steps.receive.title')),
        },
        {
          description: t(key('flow.create.steps.validate.description')),
          title: t(key('flow.create.steps.validate.title')),
        },
        {
          description: t(key('flow.create.steps.write.description')),
          title: t(key('flow.create.steps.write.title')),
        },
        {
          description: t(key('flow.create.steps.return.description')),
          title: t(key('flow.create.steps.return.title')),
        },
      ],
      retrieve: [
        {
          description: t(key('flow.retrieve.steps.enter.description')),
          title: t(key('flow.retrieve.steps.enter.title')),
        },
        {
          description: t(key('flow.retrieve.steps.cache.description')),
          title: t(key('flow.retrieve.steps.cache.title')),
        },
        {
          description: t(key('flow.retrieve.steps.lookup.description')),
          title: t(key('flow.retrieve.steps.lookup.title')),
        },
        {
          description: t(key('flow.retrieve.steps.redirect.description')),
          title: t(key('flow.retrieve.steps.redirect.title')),
        },
      ],
    };
}

function createFooter(t: Translate): FooterContent {
  return {
    relatedCases: [
      t('page.systemDesign.webhookPlatform'),
      t('page.systemDesign.googleDocs'),
      t('page.systemDesign.spotifyTrendingSongs'),
    ],
    relatedComingSoon: t(key('footer.relatedComingSoon')),
    relatedKicker: t(key('footer.relatedKicker')),
    relatedTitle: t(key('footer.relatedTitle')),
    sourceNote: t(key('footer.sourceNote')),
    takeaways: [
      t(key('footer.takeaways.item1')),
      t(key('footer.takeaways.item2')),
      t(key('footer.takeaways.item3')),
    ],
    takeawaysKicker: t(key('footer.takeawaysKicker')),
    takeawaysTitle: t(key('footer.takeawaysTitle')),
  };
}

function createHero(t: Translate): HeroContent {
  return {
      description: t(key('hero.description')),
      highlights: [
        {
          description: t(key('hero.highlights.bottleneck.description')),
          title: t(key('hero.highlights.bottleneck.title')),
        },
        {
          description: t(key('hero.highlights.apiSurface.description')),
          title: t(key('hero.highlights.apiSurface.title')),
        },
        {
          description: t(key('hero.highlights.scaling.description')),
          title: t(key('hero.highlights.scaling.title')),
        },
      ],
      kicker: t(key('hero.kicker')),
      learnKicker: t(key('hero.learnKicker')),
      targetsNote: t(key('hero.targetsNote')),
      targetsTitle: t(key('hero.targetsTitle')),
      title: t(key('hero.title')),
      visual: {
        chips: [
          {
            label: t(key('hero.visual.chips.db.label')),
            value: t(key('hero.visual.chips.db.value')),
          },
          {
            label: t(key('hero.visual.chips.cache.label')),
            value: t(key('hero.visual.chips.cache.value')),
          },
          {
            label: t(key('hero.visual.chips.cdn.label')),
            value: t(key('hero.visual.chips.cdn.value')),
          },
        ],
        description: t(key('hero.visual.description')),
        kicker: t(key('hero.visual.kicker')),
        nodes: [
          {
            label: t(key('hero.visual.nodes.input.label')),
            title: t(key('hero.visual.nodes.input.title')),
          },
          {
            label: t(key('hero.visual.nodes.token.label')),
            title: t(key('hero.visual.nodes.token.title')),
          },
          {
            label: t(key('hero.visual.nodes.redirect.label')),
            title: t(key('hero.visual.nodes.redirect.title')),
          },
        ],
        title: t(key('hero.visual.title')),
      },
    };
}

function createNavigation(t: Translate): NavigationContent {
  return {
    desktopTocAriaLabel: t(key('navigation.desktopTocAriaLabel')),
    inPageLabel: t(key('navigation.inPageLabel')),
    mobileTocAriaLabel: t(key('navigation.mobileTocAriaLabel')),
    progressLabel: t(key('navigation.progressLabel')),
    startReading: t(key('navigation.startReading')),
  };
}

function createRequirementGroups(t: Translate): RequirementGroup[] {
  return [
      {
        items: [
          t(key('requirements.functional.item1')),
          t(key('requirements.functional.item2')),
          t(key('requirements.functional.item3')),
        ],
        title: t(key('requirements.functional.title')),
      },
      {
        items: [
          t(key('requirements.nonFunctional.item1')),
          t(key('requirements.nonFunctional.item2')),
          t(key('requirements.nonFunctional.item3')),
        ],
        title: t(key('requirements.nonFunctional.title')),
      },
    ];
}

function createScalingCards(t: Translate): ScalingCard[] {
  return [
      {
        bullets: [
          t(key('scaling.indexing.item1')),
          t(key('scaling.indexing.item2')),
        ],
        title: t(key('scaling.indexing.title')),
      },
      {
        bullets: [
          t(key('scaling.caching.item1')),
          t(key('scaling.caching.item2')),
        ],
        title: t(key('scaling.caching.title')),
      },
      {
        bullets: [
          t(key('scaling.cdn.item1')),
          t(key('scaling.cdn.item2')),
        ],
        title: t(key('scaling.cdn.title')),
      },
      {
        bullets: [
          t(key('scaling.replicas.item1')),
          t(key('scaling.replicas.item2')),
        ],
        title: t(key('scaling.replicas.title')),
      },
    ];
}

function createSidebar(t: Translate): SidebarContent {
  return {
    currentSectionLabel: t(key('sidebar.currentSectionLabel')),
    difficultyLabel: t(key('sidebar.difficultyLabel')),
    difficultyValue: t(key('sidebar.difficultyValue')),
    scopeLabel: t(key('sidebar.scopeLabel')),
    scopeValue: t(key('sidebar.scopeValue')),
    tocKicker: t(key('sidebar.tocKicker')),
    tocTitle: t(key('sidebar.tocTitle')),
  };
}

function createTensionCards(t: Translate): TensionCard[] {
  return [
      {
        description: t(key('tensions.uniqueness.description')),
        title: t(key('tensions.uniqueness.title')),
      },
      {
        description: t(key('tensions.freshness.description')),
        title: t(key('tensions.freshness.title')),
      },
      {
        description: t(key('tensions.readHeavy.description')),
        title: t(key('tensions.readHeavy.title')),
      },
    ];
}

function createTradeOffs(t: Translate): TradeOffCard[] {
  return [
      {
        decision: t(key('tradeOffs.redirect.decision')),
        reason: t(key('tradeOffs.redirect.reason')),
      },
      {
        decision: t(key('tradeOffs.token.decision')),
        reason: t(key('tradeOffs.token.reason')),
      },
      {
        decision: t(key('tradeOffs.database.decision')),
        reason: t(key('tradeOffs.database.reason')),
      },
      {
        decision: t(key('tradeOffs.edge.decision')),
        reason: t(key('tradeOffs.edge.reason')),
      },
  ];
}

export function createQrCodeGeneratorPageContent(
  t: Translate,
): QrCodeGeneratorPageContent {
  const sectionBundle = createSectionBundle(t);
  const glossary = createGlossary(t);
  const ui = createUi(t, glossary);

  return {
    ...sectionBundle,
    apiCards: createApiCards(t, ui.responseLabel),
    badges: createBadges(t),
    designTargets: createDesignTargets(t),
    flowOptions: createFlowOptions(t),
    flowSteps: createFlowSteps(t),
    footer: createFooter(t),
    glossary,
    hero: createHero(t),
    navigation: createNavigation(t),
    requirementGroups: createRequirementGroups(t),
    scalingCards: createScalingCards(t),
    sidebar: createSidebar(t),
    tensionCards: createTensionCards(t),
    tradeOffs: createTradeOffs(t),
    ui,
  };
}
